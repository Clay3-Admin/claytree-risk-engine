namespace Claytree.Risk.Functions.Interface
{
    public interface INameMatchService
    {
        double ComputeScore(string inputName, string panName);
    }
}
