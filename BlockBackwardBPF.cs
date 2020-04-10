using System;
using Microsoft.Xrm.Sdk;
using CrmEarlyBound;
using Microsoft.Xrm.Sdk.Query;

namespace Block_Backward_Business_Process_Flow_Movement
{
    public class BackwardBPFStagePreventionPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IPluginExecutionContext context = (IPluginExecutionContext)
            serviceProvider.GetService(typeof(IPluginExecutionContext));

            //These lines are intended to check whether the plugin step was registered with the correct message and entity. 
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity &&
                ((Entity)context.InputParameters["Target"]).LogicalName.Equals("new_opportunityprocessflow"))
            {
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                new_opportunityprocessflow ToUpdateOppProcessFlow = (context.InputParameters["Target"] as Entity).ToEntity<new_opportunityprocessflow>();
                new_opportunityprocessflow PreUpdateOppProcessFlow = (context.PreEntityImages["a"] as Entity).ToEntity<new_opportunityprocessflow>();
                
                //Get the Process Stage record of the Opportunity Process Flow pre-update record
                string preupdate = @"
                <fetch version = '1.0' output-format = 'xml - platform' mapping = 'logical' distinct = 'true'>
                    <entity name = 'processstage'>
                        <attribute name = 'stagename' />
                        <link-entity name = 'new_opportunityprocessflow' from = 'activestageid' to = 'processstageid' link-type = 'inner' alias = 'ab'>
                            <filter type = 'and'>
                                <condition attribute = 'activestageid' operator= 'eq' uiname = 'develop' uitype = 'new_opportunityprocessflow' value = '{zzz}'/>
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

                preupdate = preupdate.Replace("zzz", PreUpdateOppProcessFlow.ActiveStageId.Id.ToString());
                EntityCollection result1 = service.RetrieveMultiple(new FetchExpression(preupdate));
                Entity preupdateprocessstage = result1[0];

                //Get the Process Stage record of the Opportunity Process Flow data for update record
                string toupdate = @"
                <fetch version = '1.0' output-format = 'xml - platform' mapping = 'logical' distinct = 'true'>
                    <entity name = 'processstage'>
                        <attribute name = 'stagename' />
                        <link-entity name = 'new_opportunityprocessflow' from = 'activestageid' to = 'processstageid' link-type = 'inner' alias = 'ab'>
                            <filter type = 'and'>
                                <condition attribute = 'activestageid' operator= 'eq' uiname = 'develop' uitype = 'new_opportunityprocessflow' value = '{zzz}'/>
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

                toupdate = toupdate.Replace("zzz", ToUpdateOppProcessFlow.ActiveStageId.Id.ToString());
                EntityCollection result2 = service.RetrieveMultiple(new FetchExpression(toupdate));
                Entity toupdateprocessstage = result2[0];
                
                try
                {
                    if (preupdateprocessstage["stagename"].ToString().ToLower() == "propose")
                    {
                        if(toupdateprocessstage["stagename"].ToString().ToLower() == "develop")
                        {
                            throw new InvalidPluginExecutionException("Moving the process from Propose to Develop stage is not allowed.");
                        }
                    }

                    if (preupdateprocessstage["stagename"].ToString().ToLower() == "close")
                    {
                        if (toupdateprocessstage["stagename"].ToString().ToLower() == "propose")
                        {
                            throw new InvalidPluginExecutionException("Moving the process from Close to Propose stage is not allowed.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    tracingService.Trace(ex.ToString());
                    throw;
                }

            }
        }
    }
}
