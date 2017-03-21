using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Pipelines.HasPresentation;
using Sitecore.Publishing;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Specialized;
using System.Globalization;

namespace Sitecore.Support.Shell.Applications.WebEdit.Commands
{
  

  [Serializable]
  public class OpenExperienceEditor : Command
  {
    public override void Execute(CommandContext context)
    {
      Assert.ArgumentNotNull(context, "context");
      NameValueCollection parameters = new NameValueCollection();
      bool flag = false;
      if (context.Items.Length == 1)
      {
        Item item = context.Items[0];
        parameters["uri"] = item.Uri.ToString();
        parameters.Add("sc_lang", item.Language.ToString());
        parameters.Add("sc_version", item.Version.Number.ToString(CultureInfo.InvariantCulture));
        if (HasPresentationPipeline.Run(item))
        {
          parameters.Add("sc_itemid", item.ID.ToString());
        }
        else
        {
          flag = true;
        }
      }
      ClientPipelineArgs args = new ClientPipelineArgs(parameters);
      if (!flag)
      {
        args.Result = "yes";
        args.Parameters.Add("needconfirmation", "false");
      }
      Context.ClientPage.Start(this, "Run", args);
    }

    public override CommandState QueryState(CommandContext context)
    {
      Assert.ArgumentNotNull(context, "context");
      if ((!UIUtil.IsIE() || (UIUtil.GetBrowserMajorVersion() >= 7)) && Settings.WebEdit.Enabled)
      {
        return base.QueryState(context);
      }
      return CommandState.Hidden;
    }

    protected void Run(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (SheerResponse.CheckModified())
      {
        if ((args.Parameters["needconfirmation"] == "false") || args.IsPostBack)
        {
          if (args.Result != "no")
          {
            UrlString str = new UrlString("/");
            str.Add("sc_mode", "edit");
            if (!string.IsNullOrEmpty(args.Parameters["sc_itemid"]))
            {
              str.Add("sc_itemid", args.Parameters["sc_itemid"]);
            }
            if (!string.IsNullOrEmpty(args.Parameters["sc_version"]))
            {
              str.Add("sc_version", args.Parameters["sc_version"]);
            }
            SiteContext previewSiteContext = null;
            if (!string.IsNullOrEmpty(args.Parameters["uri"]))
            {
              Item item = Database.GetItem(ItemUri.Parse(args.Parameters["uri"]));
              if (item == null)
              {
                SheerResponse.Alert("Item not found.", new string[0]);
                return;
              }
              previewSiteContext = LinkManager.GetPreviewSiteContext(item);
            }
            SiteContext site = previewSiteContext ?? Factory.GetSite(Settings.Preview.DefaultSite);
            if (site == null)
            {
              object[] parameters = new object[] { Settings.Preview.DefaultSite };
              SheerResponse.Alert(Translate.Text("Site \"{0}\" not found", parameters), new string[0]);
            }
            else
            {
              string str2 = args.Parameters["sc_lang"];
              if (string.IsNullOrEmpty(str2))
              {
                str2 = WebEditUtility.ResolveContentLanguage(site).ToString();
              }
              if (!string.IsNullOrEmpty(args.Parameters["sc_lang"]))
              {
                str.Add("sc_lang", str2);
              }
              PreviewManager.RestoreUser();
              Context.ClientPage.ClientResponse.Eval("window.open('" + str + "', '_blank')");
            }
          }
        }
        else
        {
          SheerResponse.Confirm("The current item does not have a layout for the current device.\n\nDo you want to open the start Web page instead?");
          args.WaitForPostBack();
        }
      }
    }
  }
}
