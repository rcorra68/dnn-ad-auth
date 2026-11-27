<%@ Control Language="C#" AutoEventWireup="false" CodeBehind="Settings.ascx.cs" Inherits="Vvf.Authentication.Dipvvf.Settings" %>

<div class="dnnForm dnnClear">

    <div class="dnnFormItem">
        <asp:Label ID="lblEnabled" runat="server" AssociatedControlID="chkEnabled" resourcekey="Enabled" CssClass="dnnFormLabel" />
        <asp:CheckBox ID="chkEnabled" runat="server" />
    </div>

    <div class="dnnFormItem">
        <asp:Label ID="lblDomainName" runat="server" AssociatedControlID="txtDomain" resourcekey="Domain" CssClass="dnnFormLabel" />
        <asp:TextBox ID="txtDomain" runat="server" />
    </div>

</div>
