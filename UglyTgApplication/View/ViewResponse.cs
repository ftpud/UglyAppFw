namespace UglyTgApplication.View;

public class ViewResponse
{
    public ResponseData[] ResponseMessages { get; set; }

    public virtual ResponseData[] GetResponse()
    {
        return ResponseMessages;
    }
}