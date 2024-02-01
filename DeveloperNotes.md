# Notes:

using Microsoft.AspNetCore.Components.Forms; InputFile / download
using Microsoft.Maui.Storage; IFilePicker / namespace CommunityToolkit.Maui.Storage; interface IFileSaver
using Microsoft.Win32; OpenFileDialog / SaveFileDialog
using System.Windows.Forms; OpenFileDialog / SaveFileDialog

---------------------------------------------------------------------------------------------------

Blazor magic strings: blazor-error-ui , --blazor-load-percentage , --blazor-load-percentage-text , blazor-error-boundary , validation-errors , validation-message

https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/BootErrors.ts
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoPlatform.ts#L230
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Web/ErrorBoundary.cs#L48
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Forms/ValidationSummary.cs#L76
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Forms/ValidationMessage.cs#L74

The following built-in Razor components are provided by the Blazor framework:

https://learn.microsoft.com/en-us/aspnet/core/blazor/components/built-in-components?view=aspnetcore-8.0

App
AntiforgeryToken			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.antiforgerytoken?view=aspnetcore-8.0
Authentication				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.webassembly.authentication.remoteauthenticatorviewcore-1?view=aspnetcore-8.0
AuthorizeView				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.authorization.authorizeview?view=aspnetcore-8.0
CascadingValue				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.cascadingvalue-1?view=aspnetcore-8.0
DataAnnotationsValidator	https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.dataannotationsvalidator?view=aspnetcore-8.0
DynamicComponent			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.dynamiccomponent?view=aspnetcore-8.0
Editor<T>					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.editor-1?view=aspnetcore-8.0
EditForm					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.editform?view=aspnetcore-8.0
<form>
ErrorBoundary				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.errorboundary?view=aspnetcore-8.0
FocusOnNavigate				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routing.focusonnavigate?view=aspnetcore-8.0
HeadContent					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.headcontent?view=aspnetcore-8.0
<head>
HeadOutlet					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.headoutlet?view=aspnetcore-8.0
InputCheckbox				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputcheckbox?view=aspnetcore-8.0
<input type="checkbox">
InputDate					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputdate-1?view=aspnetcore-8.0
<input type="date">
InputFile					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputfile?view=aspnetcore-8.0
<input type="file">
InputNumber					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputnumber-1?view=aspnetcore-8.0
<input type="number">
InputRadio					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputradio-1?view=aspnetcore-8.0
<input type="radio">
InputRadioGroup				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputradiogroup-1?view=aspnetcore-8.0
<fieldset>
InputSelect					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputselect-1?view=aspnetcore-8.0
<select>
InputText					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputtext?view=aspnetcore-8.0
<input type="text">
InputTextArea				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputtextarea?view=aspnetcore-8.0
<textarea>
LayoutView					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.layoutview?view=aspnetcore-8.0
MainLayout
NavLink						https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routing.navlink?view=aspnetcore-8.0
<a>
NavMenu
<nav>
PageTitle					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.pagetitle?view=aspnetcore-8.0
<title>
QuickGrid					https://learn.microsoft.com/en-us/aspnet/core/blazor/components/quickgrid?view=aspnetcore-8.0
Router						https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routing.router?view=aspnetcore-8.0
RouteView					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routeview?view=aspnetcore-8.0
SectionContent				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.sections.sectioncontent?view=aspnetcore-8.0
SectionOutlet				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.sections.sectionoutlet?view=aspnetcore-8.0
ValidationSummary			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.validationsummary?view=aspnetcore-8.0
Virtualize					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.virtualization.virtualize-1?view=aspnetcore-8.0
