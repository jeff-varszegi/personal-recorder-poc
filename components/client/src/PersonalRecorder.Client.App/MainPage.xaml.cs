namespace PersonalRecorder.Client.App;

/// <summary>
/// Template placeholder. The real shell is a thin layer over the capture core
/// (docs/ARCHITECTURE.md §4.3): screens bind to view-models in Client.Core and consume
/// IRecordingEngine / IPanicPipeline — never a camera API directly.
/// </summary>
public partial class MainPage : ContentPage
{
    private int _count;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnCounterClicked(object? sender, EventArgs e)
    {
        _count++;

        CounterBtn.Text = _count == 1
            ? $"Clicked {_count} time"
            : $"Clicked {_count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
}
