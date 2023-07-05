/* $Rev: 29788 $ */
using System.Xml.Serialization;
using System.Collections.Generic;

[System.SerializableAttribute()]
[System.Xml.Serialization.XmlRootAttribute("ErrorResponse", IsNullable = false)]
public partial class ErrorResponseType
{
    private static IDictionary<int, string> errors = new Dictionary<int, string>
    {
        { 200, "Succès." },
        { 204, "Aucun résultat." },
        { 303, "Redirection." },
        { 400, "Paramètres d'entrée incorrects." },
        { 401, "La langue passée en paramètre n'est pas supportée." },
        { 500, "Erreur serveur." },
        { 501, "Pas implémenté." },
        { 503, "Service indisponible." }
    };

    private int codeNumber;
    private string codeField;
    private string messageField;

    public ErrorResponseType() { }

    public ErrorResponseType(int code)
    {
        this.codeNumber = code;
        this.codeField = code.ToString();
        errors.TryGetValue(code, out this.messageField);
    }

    public int GetCode()
    {
        return this.codeNumber;
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "normalizedString")]
    public string code
    {
        get
        {
            return this.codeField;
        }
        set
        {
            this.codeField = value;
        }
    }

    [System.Xml.Serialization.XmlElementAttribute(DataType = "normalizedString")]
    public string message
    {
        get
        {
            return this.messageField;
        }
        set
        {
            this.messageField = value;
        }
    }
}
