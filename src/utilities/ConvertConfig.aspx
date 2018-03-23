<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ConvertConfig.aspx.cs" Inherits="ConvertConfig" %>
<!-- $Rev: 14634 $ -->
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <asp:Label runat="server">URL du site eCadastre:</asp:Label>
            <asp:TextBox runat="server" ID="SiteUrl" Width="400">https://ge.ch/terecadastre</asp:TextBox>
            <div style="margin-top: 10px">
                <asp:Button runat="server" Name="ConvertBtn" Text="Convertir" OnClick="ConvertBtn_Click" />
            </div>
            <div style="margin-top: 10px">
                <asp:TextBox runat="server" ID="Result" TextMode="MultiLine" Columns="100" Rows="5" />
            </div>
        </div>
    </form>
</body>
</html>
