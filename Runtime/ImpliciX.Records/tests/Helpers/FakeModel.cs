using ImpliciX.Language.Model;

namespace ImpliciX.Records.Tests.Helpers;

public sealed class model : RootModelNode
{
    public static RecordsNode<TheAlarm> the_alarms_b { get; }
    public static RecordsNode<TheAlarm> the_alarms { get; }
    public static RecordWriterNode<TheAlarm> alarm { get; }
    public static RecordWriterNode<TheAlarm> alarm_copy { get; }
    public static RecordWriterNode<TheAlarm> other_alarm { get; }
    
    public static RecordsNode<RecordWithTextFields> text_record { get; }
    public static RecordWriterNode<RecordWithTextFields> text_record_writer { get; }

    static model()
    {
        var modelNode = new model();
        the_alarms = new RecordsNode<TheAlarm>(nameof(the_alarms), modelNode);
        the_alarms_b = new RecordsNode<TheAlarm>(nameof(the_alarms_b), modelNode);
        alarm = new RecordWriterNode<TheAlarm>(nameof(alarm), modelNode, (n, p) => new TheAlarm(n, p));
        alarm_copy = new RecordWriterNode<TheAlarm>(nameof(alarm), modelNode, (n, p) => new TheAlarm(n, p));
        other_alarm = new RecordWriterNode<TheAlarm>(nameof(other_alarm), modelNode, (n, p) => new TheAlarm(n, p));
        text_record = new RecordsNode<RecordWithTextFields>(nameof(text_record), modelNode);
        text_record_writer = new RecordWriterNode<RecordWithTextFields>(nameof(text_record_writer), modelNode, (n, p) => new RecordWithTextFields(n, p));
    }

    private model() : base(nameof(model)) { }
}

public enum TheAlarmKind
{
    Error1,
    Error2,
    Error3
}

public sealed class RecordWithTextFields : ModelNode
{
    public PropertyUrn<Literal> literal { get; }
    public PropertyUrn<Text10> text_10 { get; }
    public PropertyUrn<Text50> text_50 { get; }
    public PropertyUrn<Text200> text_200 { get; }

    public RecordWithTextFields(string name, ModelNode parent) : base(name, parent)
    {
        literal = PropertyUrn<Literal>.Build(Urn, nameof(literal));
        text_10 = PropertyUrn<Text10>.Build(Urn, nameof(text_10));
        text_50 = PropertyUrn<Text50>.Build(Urn, nameof(text_50));
        text_200 = PropertyUrn<Text200>.Build(Urn, nameof(text_200));
    }
}
    

public sealed class TheAlarm : ModelNode
{
    public PropertyUrn<TheAlarmKind> kind { get; }
    public PropertyUrn<Temperature> some_value { get; }
    public NestedTheAlarm nested_alarm { get; }

    public TheAlarm(string name, ModelNode parent) : base(name, parent)
    {
        kind = PropertyUrn<TheAlarmKind>.Build(Urn, nameof(kind));
        some_value = PropertyUrn<Temperature>.Build(Urn, nameof(some_value));
        nested_alarm = new NestedTheAlarm(nameof(nested_alarm), this);
    }
}

public sealed class NestedTheAlarm : ModelNode
{
    public PropertyUrn<Temperature> nested_temp { get; }

    public NestedTheAlarm(string urnToken, ModelNode parent) : base(urnToken, parent)
    {
        nested_temp = PropertyUrn<Temperature>.Build(Urn, nameof(nested_temp));
    }
}