namespace SimilarHighlight.OutputWindow
{
    public interface IOutputWindowService
    {
        IOutputWindowPane TryGetPane(string name);
    }
}
