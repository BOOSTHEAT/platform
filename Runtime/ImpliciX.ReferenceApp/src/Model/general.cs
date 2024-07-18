using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model;

public class general : RootModelNode
{
    public static GuiNode swipe_node { get; }
    public static GuiNode main_screen { get; }
    public static GuiNode zoom_pie_input { get; }
    public static GuiNode zoom_pie_output { get; }
    public static GuiNode zoom_bar_input { get; }
    public static GuiNode zoom_bar_output { get; }
    public static GuiNode zoom_time_lines { get; }
    public static GuiNode zoom_time_bars { get; }
    public static GuiNode data_screen { get; }
    
    public static RecordsNode<Report> report_records_node { get; }
    public static RecordWriterNode<Report> report_record_writer { get; }

    static general()
    {
        main_screen = new GuiNode(new general(), nameof(main_screen));
        zoom_pie_input = new GuiNode(new general(), nameof(zoom_pie_input));
        zoom_pie_output = new GuiNode(new general(), nameof(zoom_pie_output));
        zoom_bar_input = new GuiNode(new general(), nameof(zoom_bar_input));
        zoom_bar_output = new GuiNode(new general(), nameof(zoom_bar_output));
        zoom_time_lines = new GuiNode(new general(), nameof(zoom_time_lines));
        zoom_time_bars = new GuiNode(new general(), nameof(zoom_time_bars));
        data_screen = new GuiNode(new general(), nameof(data_screen));
        swipe_node = new GuiNode(new general(), nameof(swipe_node));
        
        report_records_node = new RecordsNode<Report>(nameof(report_records_node), new general());
        report_record_writer = new RecordWriterNode<Report>(nameof(report_record_writer), new general(), (n, p) => new Report(n, p));
    }

    private general() : base(nameof(general))
    {
    }
}

public class Report : ModelNode
{
    public Report(string name, ModelNode parent) : base(name, parent)
    {
        title = PropertyUrn<Literal>.Build(Urn, nameof(title));
        summary = PropertyUrn<Literal>.Build(Urn, nameof(summary));
    }

    public PropertyUrn<Literal> title { get; }
    public PropertyUrn<Literal> summary { get; }
}