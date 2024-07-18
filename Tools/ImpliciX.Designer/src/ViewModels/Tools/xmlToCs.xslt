<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt">
    <xsl:param name="namespace">MissingNamespace</xsl:param>
    <xsl:param name="valueTypes"/>
    <xsl:output method="text" />

    <xsl:template match="/">
    <xsl:text>
      using ImpliciX.Language.GUI;
      using ImpliciX.Language.Model;
      using Time = ImpliciX.Language.Model.Duration;
      using SubSystem = ImpliciX.Language.Model.SubSystemNode;
    </xsl:text>
        namespace <xsl:value-of select="$namespace" />; <xsl:apply-templates />
    </xsl:template>

    <xsl:template match="root">
        public class <xsl:value-of select="@name" /> : RootModelNode {
        <xsl:apply-templates mode="declare_static" />
        static <xsl:value-of select="@name" />() {
        var self = new <xsl:value-of select="@name" />();
        <xsl:apply-templates mode="construct" />
        }
        private <xsl:value-of select="@name" />() : base(nameof(<xsl:value-of select="@name" />))
        {
        }
        }
    </xsl:template>

    <xsl:template match="subgroup" mode="declare">
        <xsl:call-template name="declare_subgroup" />
    </xsl:template>

    <xsl:template match="subgroup" mode="declare_static">
        <xsl:call-template name="declare_subgroup">
            <xsl:with-param name="qualifier">static </xsl:with-param>
        </xsl:call-template>
    </xsl:template>

    <xsl:template name="declare_subgroup">
        <xsl:param name="qualifier">&#32;</xsl:param>
        <xsl:variable name="subgroupclass">Internal<xsl:value-of select="@itemname" /></xsl:variable>
        <xsl:variable name="parentclass">Internal<xsl:value-of select="@itemname" /></xsl:variable>
        public <xsl:value-of select="$qualifier" /> <xsl:value-of select="$subgroupclass" /> <xsl:text> </xsl:text> <xsl:value-of select="@itemname" /> { get; }

        <xsl:call-template name="declare_class">
            <xsl:with-param name="classname"><xsl:value-of select="$subgroupclass" /></xsl:with-param>
        </xsl:call-template>
    </xsl:template>

    <xsl:template match="subgroup" mode="construct"><xsl:value-of select="@itemname" /> = new Internal<xsl:value-of
            select="@itemname" />(nameof(<xsl:value-of select="@itemname" />), self); </xsl:template>

    <xsl:template match="enum">
        [ValueObject]
        public enum <xsl:value-of select="@type" /> {
        <xsl:apply-templates select="enumvalue" />
        }
    </xsl:template>

    <xsl:template match="enumvalue">
        <xsl:value-of select="@name" /> = <xsl:value-of select="@value" />, </xsl:template>

    <xsl:template match="group">
        <xsl:call-template name="declare_class">
            <xsl:with-param name="classname"><xsl:value-of select="@typename" /></xsl:with-param>
        </xsl:call-template>
    </xsl:template>

    <xsl:template name="declare_class">
        <xsl:param name="classname"/>
        <xsl:variable name="parentclassname">
            <xsl:choose>
                <xsl:when test="groupparent/@parent">
                    <xsl:call-template name="get_type_declaration">
                        <xsl:with-param name="type"><xsl:value-of select="groupparent/@parent" /></xsl:with-param>
                        <xsl:with-param name="genericArg"><xsl:value-of select="groupparent/generic/@type" /></xsl:with-param>
                    </xsl:call-template>
                </xsl:when>
                <xsl:otherwise>ModelNode</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
    public class <xsl:value-of select="$classname" /> : <xsl:value-of select="$parentclassname" /> {
        public <xsl:value-of select="$classname" />(string name, ModelNode parent) : base(name, parent) {
        var self = this;
        <xsl:if test="$parentclassname='BurnerNode'">
            fan = new <xsl:value-of select="groupparent/generic[2]/@type" />(nameof(fan), self);
            burner_fan = fan;
        </xsl:if>
        <xsl:apply-templates mode="construct" />
        }
        <xsl:if test="$parentclassname='BurnerNode'">
            public <xsl:value-of select="groupparent/generic[2]/@type" /> fan { get; }
        </xsl:if>
        <xsl:apply-templates mode="declare" />
    }
    </xsl:template>

    <xsl:template match="item" mode="declare">
        <xsl:call-template name="declare_item" />
    </xsl:template>
    <xsl:template match="item" mode="declare_static">
        <xsl:call-template name="declare_item">
            <xsl:with-param name="qualifier">static </xsl:with-param>
        </xsl:call-template>
    </xsl:template>
    <xsl:template name="declare_item">
        <xsl:param name="qualifier">&#32;</xsl:param> public <xsl:value-of select="$qualifier" /> <xsl:call-template
            name="itemType" /> <xsl:text> </xsl:text> <xsl:value-of select="@itemname" /> { get; } </xsl:template>
    <xsl:template name="itemType">
        <xsl:call-template name="get_type_declaration">
            <xsl:with-param name="type" select="@type" />
        </xsl:call-template>
    </xsl:template>

    <xsl:template match="item" mode="construct">
        <xsl:variable name="theType" select="@type" />
        <xsl:value-of select="@itemname" /> = <xsl:choose>
        <xsl:when test="$valueTypes/type[text()=$theType]">PropertyUrn&lt;<xsl:value-of select="@type" />&gt;.Build(Urn, nameof(<xsl:value-of select="@itemname" />))</xsl:when>
        <xsl:when test="@type='Measure'">new MeasureNode&lt;<xsl:value-of select="generic/@type" />&gt;(nameof(<xsl:value-of select="@itemname" />), self)</xsl:when>
        <xsl:when test="@type='ManuelAlert'">new ManualAlert&lt;<xsl:value-of select="generic/@type" />&gt;(nameof(<xsl:value-of select="@itemname" />), self)</xsl:when>
        <xsl:when test="@type='Command'">CommandUrn&lt;<xsl:call-template name="get_command_arg" />&gt;.Build(Urn, nameof(<xsl:value-of select="@itemname" />))</xsl:when>
        <xsl:when test="@type='CommandN'">CommandNode&lt;<xsl:call-template name="get_command_arg" />&gt;.Create(nameof(<xsl:value-of select="@itemname" />), self)</xsl:when>
        <xsl:when test="@type='VersionSetting'">VersionSettingUrn&lt;<xsl:value-of select="generic/@type" />&gt;.Build(Urn, nameof(<xsl:value-of select="@itemname" />))</xsl:when>
        <xsl:when test="@type='UserSetting'">UserSettingUrn&lt;<xsl:value-of select="generic/@type" />&gt;.Build(Urn, nameof(<xsl:value-of select="@itemname" />))</xsl:when>
        <xsl:when test="@type='PersitentCounter'">PersistentCounterUrn&lt;<xsl:value-of select="generic/@type" />&gt;.Build(Urn, nameof(<xsl:value-of select="@itemname" />))</xsl:when>
        <xsl:when test="@type='Metric'">MetricUrn.Build(Urn, nameof(<xsl:value-of select="@itemname" />))</xsl:when>
        <xsl:when test="@type='SubSystemX'">new SubSystem(nameof(<xsl:value-of select="@itemname" />), self)</xsl:when>
        <xsl:when test="@type='Screen'">new GuiNode(self, nameof(<xsl:value-of select="@itemname" />))</xsl:when>
        <xsl:when test="@type='Records'">new RecordsNode&lt;<xsl:value-of select="generic/@type" />&gt;(nameof(<xsl:value-of select="@itemname" />), self)</xsl:when>
        <xsl:when test="@type='RecordWriter'">new RecordWriterNode&lt;<xsl:value-of select="generic/@type" />&gt;(nameof(<xsl:value-of select="@itemname" />), self, (n,p) => new <xsl:value-of select="generic/@type" />(n,p))</xsl:when>
        <xsl:otherwise>new <xsl:value-of select="@type" />(nameof(<xsl:value-of select="@itemname" />), self)</xsl:otherwise>
    </xsl:choose>;</xsl:template>
    
    <xsl:template name="get_command_arg">
        <xsl:choose>
            <xsl:when test="generic/@type"><xsl:value-of select="generic/@type" /></xsl:when>
            <xsl:otherwise>NoArg</xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="get_type_declaration">
        <xsl:param name="type" />
        <xsl:param name="genericArg" select="msxsl:node-set($type)/../generic/@type" />
        <xsl:choose>
            <xsl:when test="$valueTypes/type[text()=$type]">PropertyUrn&lt;<xsl:value-of select="$type" />&gt;</xsl:when>
            <xsl:when test="$type='SubSystem'">SubSystem</xsl:when>
            <xsl:when test="$type='Burner'">BurnerNode</xsl:when>
            <xsl:when test="$type='Fan'">FanNode</xsl:when>
            <xsl:when test="$type='Motor'">MotorNode</xsl:when>
            <xsl:when test="$type='Measure'">MeasureNode&lt;<xsl:value-of select="$genericArg" />&gt;</xsl:when>
            <xsl:when test="$type='ManuelAlert'">ManualAlert&lt;<xsl:value-of select="$genericArg" />&gt;</xsl:when>
            <xsl:when test="$type='Command'">CommandUrn&lt;<xsl:call-template name="get_command_arg" />&gt;</xsl:when>
            <xsl:when test="$type='CommandN'">CommandNode&lt;<xsl:value-of select="$genericArg" />&gt;</xsl:when>
            <xsl:when test="$type='VersionSetting'">VersionSettingUrn&lt;<xsl:value-of select="$genericArg" />&gt;</xsl:when>
            <xsl:when test="$type='UserSetting'">UserSettingUrn&lt;<xsl:value-of select="$genericArg" />&gt;</xsl:when>
            <xsl:when test="$type='PersitentCounter'">PersistentCounterUrn&lt;<xsl:value-of select="$genericArg" />&gt;</xsl:when>
            <xsl:when test="$type='Metric'">MetricUrn</xsl:when>
            <xsl:when test="$type='SubSystemX'">SubSystem</xsl:when>
            <xsl:when test="$type='Screen'">GuiNode</xsl:when>
            <xsl:when test="$type='Records'">RecordsNode&lt;<xsl:value-of select="$genericArg" />&gt;</xsl:when>
            <xsl:when test="$type='RecordWriter'">RecordWriterNode&lt;<xsl:value-of select="$genericArg" />&gt;</xsl:when>
            <xsl:otherwise><xsl:value-of select="$type" /></xsl:otherwise>
        </xsl:choose>
    </xsl:template>

</xsl:stylesheet>
