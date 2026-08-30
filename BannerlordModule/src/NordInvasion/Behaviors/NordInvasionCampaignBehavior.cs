using TaleWorlds.CampaignSystem;

namespace NordInvasion.Behaviors
{
    public class NordInvasionCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Campaign map logic - голосование деревень через NI_CampaignMap_VM
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
