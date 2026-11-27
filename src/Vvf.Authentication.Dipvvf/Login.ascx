<%@ Control Language="C#" AutoEventWireup="false" CodeBehind="Login.ascx.cs" Inherits="Vvf.Authentication.Dipvvf.Login" %>

<asp:Panel ID="pnlLogin" runat="server" DefaultButton="cmdLogin" CssClass="dnnForm dnnLoginService dnnClear">

    <asp:Label ID="lblMessage" runat="server" Visible="false" CssClass="dnnFormMessage dnnFormValidationSummary" />

    <div class="dnnFormItem">
        <asp:Label ID="lblUsername" runat="server" AssociatedControlID="txtUsername" resourcekey="Username" CssClass="dnnFormLabel" />
        <asp:TextBox ID="txtUsername" runat="server" />
        <asp:Label ID="lblDomain" runat="server" />
    </div>

    <div class="dnnFormItem">
        <asp:Label ID="lblPassword" runat="server" AssociatedControlID="txtPassword" resourcekey="Password" CssClass="dnnFormLabel" />
        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" />
    </div>

    <div class="dnnFormItem">
        <span class="dnnFormLabel"></span>
        <asp:CheckBox ID="chkRememberMe" runat="server" resourcekey="RememberMe" />
    </div>

    <div class="dnnFormItem">
        <span class="dnnFormLabel"></span>
        <asp:LinkButton ID="cmdLogin" runat="server" resourcekey="Login" CssClass="dnnPrimaryAction" />
    </div>

</asp:Panel>
