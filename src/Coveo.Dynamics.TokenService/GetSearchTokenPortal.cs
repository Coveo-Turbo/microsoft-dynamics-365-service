using System;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Coveo.Dynamics.TokenService
{
    public class GetSearchTokenPortal : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            //IOrganizationService service = serviceFactory.CreateOrganizationService(null);

            //retrieves the important environment variables.
            String fetchQuery = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>"
                                + "  <entity name='environmentvariabledefinition'>"
                                + "    <attribute name='environmentvariabledefinitionid' />"
                                + "    <attribute name='schemaname' />"
                                + "    <attribute name='defaultvalue' />"
                                + "    <order attribute='schemaname' descending='false' />"
                                + "    <filter type='or'>"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_portal_OrganizationId' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_portal_PlatformURL' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_portal_APIKey' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_portal_EmailAddressField' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_portal_AllowedSearchHubs' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_portal_Filter' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_portal_Pipeline' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_portal_ValidFor' />"
                                + "      <condition attribute='schemaname' operator='eq' value='cvo_portal_NewUseCase' />"
                                + "    </filter>"
                                + "    <link-entity name='environmentvariablevalue' from='environmentvariabledefinitionid' to='environmentvariabledefinitionid' alias='envvalue' link-type='outer' intersect='true' visible='true'>"
                                + "      <attribute name='value' />"
                                + "    </link-entity>"
                                + "  </entity>"
                                + "</fetch>";


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

            if (!ValidateSearchhub(EnvVariables["cvo_portal_AllowedSearchHubs"].ToString(), context.InputParameters["cvo_portal_SearchHub"].ToString()))
            {
                context.OutputParameters["cvo_portal_Response"] = $"SearchHub {context.InputParameters["cvo_portal_SearchHub"]} is not allowed.";
                return;
            }
            EnvVariables.Add("SearchHub", context.InputParameters["cvo_portal_SearchHub"].ToString());
            outputDict.Add("SearchHub", context.InputParameters["cvo_portal_SearchHub"].ToString());

            Dictionary<String, String> contactStrings = new Dictionary<String, String>();

            if (context.InputParameters["cvo_portal_ContactId"].ToString() != "")
            {         
                //retrieves the important environment variables.
                String fetchContactQuery = "  <fetch version='1.0' output-format='xml - platform' mapping ='logical' distinct ='false'> "
                                    + " <entity name = 'contact'>"
                                    + "     <attribute name = 'fullname'/>"
                                    + "     <attribute name = '" + EnvVariables["cvo_portal_EmailAddressField"] + "'/>"
                                    + "     <attribute name = 'contactid'/>"
                                    + "     <filter>"
                                    + "         <condition attribute = 'contactid' operator= 'eq' value = '" + context.InputParameters["cvo_portal_ContactId"].ToString() + "' />"
                                    + "     </filter>"
                                    + " </entity>"
                                    + "</fetch>";


                var contactInfo = service.RetrieveMultiple(new FetchExpression(fetchContactQuery));

                foreach (var contact in contactInfo.Entities)
                {
                    if (contact != null && contact.Attributes.ContainsKey(EnvVariables["cvo_portal_EmailAddressField"]))
                    {
                        contactStrings.Add("email", contact.Attributes[EnvVariables["cvo_portal_EmailAddressField"]].ToString());
                        if (contact.Attributes.ContainsKey("fullname"))
                            contactStrings.Add("displayName", contact.Attributes["fullname"].ToString());
                        else
                            contactStrings.Add("displayName", contact.Attributes[EnvVariables["cvo_portal_EmailAddressField"]].ToString());
                    }
                    else
                    {
                        contactStrings.Add("displayName", "Anonymous");
                        contactStrings.Add("email", "anonymous_user@anonymous.coveo.com");
                    }
                }
            }
            else
            {
                contactStrings.Add("displayName", "Anonymous");
                contactStrings.Add("email", "anonymous_user@anonymous.coveo.com");
            }

            String requestJson = BuildRequestJson(contactStrings, EnvVariables);

            String url = String.Format("{0}/v2/token?&organizationId={1}", EnvVariables["cvo_portal_PlatformURL"], EnvVariables["cvo_portal_OrganizationId"]);
            WebRequest theRequest = WebRequest.Create(url);
            theRequest.Method = "POST";

            theRequest.ContentType = "application/json";
            theRequest.ContentLength = requestJson.Length;
            theRequest.Headers.Add("Authorization", "Bearer " + EnvVariables["cvo_portal_APIKey"]);
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


            context.OutputParameters["cvo_portal_Response"] = "{"
                + "\"SearchHub\":\"" + outputDict["SearchHub"] + "\""
                + ",\"Token\":" + outputDict["Token"]   //No need for quotes around the token, since the response returns quotes
                + "}";

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

        private String BuildRequestJson(Dictionary<String, String> userInfo, Dictionary<String, String> env)
        {
            /* Those first 4 parameters will always be passed in the token.
             * userId is mandatory
             * validFor is set to the default value or the new changed one.
             */

            String jsonOutput =
                "{"
                + "\"userIds\": [{\"name\": \"" + userInfo["email"] + "\",\"type\": \"User\",\"provider\": \"Email Security Provider\"}]"
                + ",\"searchHub\": \"" + env["SearchHub"] + "\""
                + ",\"userDisplayName\": \"" + userInfo["displayName"] + "\""
                + ",\"validFor\": " + env["cvo_portal_ValidFor"];

            //We should never use the default pipeline, thus if we pass default, we bypass this
            if (env["cvo_portal_Pipeline"] != "default")
                jsonOutput += ",\"pipeline\": \"" + env["cvo_portal_Pipeline"] + "\"";

            //The default filter changes nothing, thus we only insert it if it's different
            if (env["cvo_portal_Filter"] != "@uri")
                jsonOutput += ",\"filter\": \"" + env["cvo_portal_Filter"] + "\"";

            jsonOutput += "}";

            return jsonOutput;
        }
    }
}
