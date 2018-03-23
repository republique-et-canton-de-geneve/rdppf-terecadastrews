/* $Rev: 14634 $ */
public class CommonThread
{
    protected string ErrorMessage;
    protected bool Success;

    public CommonThread()
    {
        ErrorMessage = string.Empty;
        Success = false;
    }

    public string GetErrorMessage()
    {
        return ErrorMessage;
    }

    public bool IsSuccessfull()
    {
        return Success;
    }
}