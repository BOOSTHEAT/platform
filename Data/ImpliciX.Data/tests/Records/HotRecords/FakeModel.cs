using ImpliciX.Language.Model;

namespace ImpliciX.Data.Tests.Records.HotRecords;

public sealed class model : RootModelNode
{
    public static RecordsNode<TheAlarmFiveFields> the_alarms_b { get; }
    public static RecordsNode<TheAlarmFiveFields> the_alarms { get; }
    public static RecordWriterNode<TheAlarmFiveFields> alarm { get; }
    public static RecordWriterNode<TheAlarmFiveFields> alarm_copy { get; }
    public static RecordWriterNode<TheAlarmFiveFields> other_alarm { get; }

    static model()
    {
        var modelNode = new model();
        the_alarms = new RecordsNode<TheAlarmFiveFields>(nameof(the_alarms), modelNode);
        the_alarms_b = new RecordsNode<TheAlarmFiveFields>(nameof(the_alarms_b), modelNode);
        alarm = new RecordWriterNode<TheAlarmFiveFields>(nameof(alarm), modelNode, (n, p) => new TheAlarmFiveFields(n, p));
        alarm_copy = new RecordWriterNode<TheAlarmFiveFields>(nameof(alarm), modelNode, (n, p) => new TheAlarmFiveFields(n, p));
        other_alarm = new RecordWriterNode<TheAlarmFiveFields>(nameof(other_alarm), modelNode, (n, p) => new TheAlarmFiveFields(n, p));
    }

    private model() : base(nameof(model)) { }
}

public enum TheAlarmKind
{
    Error1,
    Error2,
    Error3
}

public sealed class TheAlarmFiveFields : ModelNode
{
    public PropertyUrn<TheAlarmKind> kind { get; }
    public PropertyUrn<Temperature> float_value { get; }
    public PropertyUrn<Literal> text_value { get; }
    
    public NestedTheAlarm nested_alarm { get; }

    public TheAlarmFiveFields(string name, ModelNode parent) : base(name, parent)
    {
        kind = PropertyUrn<TheAlarmKind>.Build(Urn, nameof(kind));
        float_value = PropertyUrn<Temperature>.Build(Urn, nameof(float_value));
        nested_alarm = new NestedTheAlarm(nameof(nested_alarm), this);
        text_value = PropertyUrn<Literal>.Build(Urn, nameof(text_value));
    }
}

public sealed class TheAlarmTwoFields : ModelNode
{
    public PropertyUrn<TheAlarmKind> kind { get; }
    public PropertyUrn<Temperature> float_value { get; }
    
    public TheAlarmTwoFields(string name, ModelNode parent) : base(name, parent)
    {
        kind = PropertyUrn<TheAlarmKind>.Build(Urn, nameof(kind));
        float_value = PropertyUrn<Temperature>.Build(Urn, nameof(float_value));
    }
}

public sealed class TheAlarmThreeFields : ModelNode
{
    public PropertyUrn<TheAlarmKind> kind { get; }
    public PropertyUrn<Temperature> float_value { get; }
    
    public PropertyUrn<Literal> text_value { get; }

    public TheAlarmThreeFields(string name, ModelNode parent) : base(name, parent)
    {
        kind = PropertyUrn<TheAlarmKind>.Build(Urn, nameof(kind));
        float_value = PropertyUrn<Temperature>.Build(Urn, nameof(float_value));
        text_value = PropertyUrn<Literal>.Build(Urn, nameof(text_value));
    }
}

public sealed class NestedTheAlarm : ModelNode
{
    public PropertyUrn<Temperature> nested_temp { get; }
    public PropertyUrn<Literal> text_value { get; }

    public NestedTheAlarm(string urnToken, ModelNode parent) : base(urnToken, parent)
    {
        nested_temp = PropertyUrn<Temperature>.Build(Urn, nameof(nested_temp));
        text_value = PropertyUrn<Literal>.Build(Urn, nameof(text_value));
    }
}