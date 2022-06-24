using System;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Coveo.Dynamics.TokenService
{
    
    public class GetSearchTokenCRM :IPlugin
    {
       public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            //retrieves the important environment variables.
            String fetchQuery = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>"
                                + "  <entity name='environmentvariabledefinition'>"
                                + "    <attribute name='environmentvariabledefinitionid' />"
                                + "    <attribute name='schemaname' />"
                                + "    <attribute name='defaultvalue' />"
                                + "    <order attribute='schemaname' descending='false' />"
                                + "    <filter type='or'>"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_crm_OrganizationId' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_crm_PlatformURL' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_crm_APIKey' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_crm_EmailAddressField' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_crm_AllowedSearchHubs' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_crm_Filter' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_crm_Pipeline' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_crm_ValidFor' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_crm_NewUseCase' />"
                                + "    </filter>"
                                + "    <link-entity name='environmentvariablevalue' from='environmentvariabledefinitionid' to='environmentvariabledefinitionid' alias='envvalue' link-type='outer' intersect='true' visible='true'>"
                                + "      <attribute name='value' />"
                                + "    </link-entity>"
                                + "  </entity>"
                                + "</fetch>";

            try
            {
                var variable = service.RetrieveMultiple(new FetchExpression(fetchQuery));

                Dictionary<String, String> EnvVariables = new Dictionary<String, String>();
                Dictionary<String, String> outputDict = new Dictionary<String, String>();

                //adds all the environment variables to the dictionary
                foreach (var envVar in variable.Entities)
                {
                    //If this exists, it means the default value was redifined, thus we grab the new value
                    AliasedValue currentVal = envVar.GetAttributeValue<AliasedValue>("envvalue.value");
                    if (currentVal != null)
                        EnvVariables.Add(envVar.Attributes["schemaname"].ToString(), currentVal.Value.ToString());
                    else
                        EnvVariables.Add(envVar.Attributes["schemaname"].ToString(), envVar.Attributes["defaultvalue"].ToString());
                }

                if (!ValidateSearchhub(EnvVariables["cvo_crm_AllowedSearchHubs"].ToString(), context.InputParameters["cvo_crm_SearchHub"].ToString()))
                {
                    context.OutputParameters["cvo_crm_Response"] = $"SearchHub {context.InputParameters["cvo_crm_SearchHub"]} is not allowed.";
                    return;
                }
                EnvVariables.Add("SearchHub", context.InputParameters["cvo_crm_SearchHub"].ToString());
                outputDict.Add("SearchHub", context.InputParameters["cvo_crm_SearchHub"].ToString());

                var initUser = context.InitiatingUserId;
                // Lookup User to retrieve email
                Entity UserInfo = service.Retrieve("systemuser", initUser, new ColumnSet(new string[] { "fullname", EnvVariables["cvo_crm_EmailAddressField"] }));
   
                /*
                String values = "";
                foreach (KeyValuePair<String, String> kvp in EnvVariables)
                {
                    values += (kvp.Key, kvp.Value);
                }
                */

                String requestJson = BuildRequestJson(UserInfo, EnvVariables);

                String url = String.Format("{0}/v2/token?&organizationId={1}", EnvVariables["cvo_crm_PlatformURL"], EnvVariables["cvo_crm_OrganizationId"]);
                WebRequest theRequest = WebRequest.Create(url);
                theRequest.Method = "POST";

                theRequest.ContentType = "application/json";
                theRequest.ContentLength = requestJson.Length;
                theRequest.Headers.Add("Authorization", "Bearer "+ EnvVariables["cvo_crm_APIKey"]);
                Stream requestStream = theRequest.GetRequestStream();

                requestStream.Write(Encoding.ASCII.GetBytes(requestJson), 0, requestJson.Length);
                requestStream.Close();

                HttpWebResponse response = (HttpWebResponse)theRequest.GetResponse();

                using (Stream dataStream = response.GetResponseStream())
                {
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    String responseFromServer = reader.ReadToEnd();
                    String[] tokenResponse = responseFromServer.Split(':');
                    outputDict.Add("Token", tokenResponse[1].Replace('}', ' '));
                }

                // Close the response.
                response.Close();


                context.OutputParameters["cvo_crm_Response"] = "{"
                    + "\"SearchHub\":\"" + outputDict["SearchHub"] + "\""
                    + ",\"Token\":" + outputDict["Token"]   //No need for quotes around the token, since the response returns quotes
                    + "}";

            }
            catch (Exception e)
            {
                context.OutputParameters["cvo_crm_Response"] = ("{0} Exception caught.", e);
            }
        }

        private bool ValidateSearchhub(String allowedHubs, String currentHub)
        {
            // Since the SearchHub is mandatory, validate against the AllowedSearchHubs passed in Env Variables
            String[] hubsList = allowedHubs.ToLower().Split(',');

            bool hubIsValid = false;

            for (int i = 0; i < hubsList.Length; i++)
            {
                if (hubsList[i].Trim(' ') == currentHub.ToLower().Trim(' '))
                {
                    hubIsValid = true;
                }
            }

            return hubIsValid;
        }

        private String BuildRequestJson(Entity userInfo, Dictionary<String, String> env)
        {
            /* Those first 4 parameters will always be passed in the token.
             * userId is mandatory
             * validFor is set to the default value or the new changed one.
             */

            String email = "";
            if (userInfo != null && userInfo.Attributes.ContainsKey(env["cvo_crm_EmailAddressField"]))
                email = userInfo[env["cvo_crm_EmailAddressField"]].ToString();
            else
                email = "anonymous_user@anonymous.coveo.com";

            String displayName = "";
            if (userInfo != null && userInfo.Attributes.ContainsKey("fullname"))
                displayName = userInfo["fullname"].ToString();
            else
                displayName = "Anonymous";


            String jsonOutput =
                "{"
                + "\"userIds\": [{\"name\": \"" + email + "\",\"type\": \"User\",\"provider\": \"Email Security Provider\"}]"
                + ",\"searchHub\": \"" + env["SearchHub"] + "\""
                + ",\"userDisplayName\": \"" + displayName + "\""
                + ",\"validFor\": " + env["cvo_crm_ValidFor"];
            
            //We should never use the default pipeline, thus if we pass default, we bypass this
            if(env["cvo_crm_Pipeline"] != "default")
                jsonOutput += ",\"pipeline\": \"" + env["cvo_crm_Pipeline"] + "\"";

            //The default filter changes nothing, thus we only insert it if it's different
            if (env["cvo_crm_Filter"] != "@uri")
                jsonOutput += ",\"filter\": \"" + env["cvo_crm_Filter"] + "\"";

            jsonOutput += "}";

            return jsonOutput;
        }
    }
}
