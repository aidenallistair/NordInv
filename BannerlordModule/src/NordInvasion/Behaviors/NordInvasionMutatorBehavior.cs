using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using NordInvasion.Models;

namespace NordInvasion.Behaviors
{
    public class NordInvasionMutatorBehavior : MissionBehavior
    {
        public Models.MutatorType CurrentMutator = Models.MutatorType.None;

        public void ApplyMutator(Models.MutatorType mutator)
        {
            CurrentMutator = mutator;
            var def = Models.MutatorDatabase.All.Find(m => m.Type == mutator);
            InformationManager.DisplayMessage(new InformationMessage(
                $"MUTATOR: {(def != null ? def.Name + " (" + def.God + ")" : mutator.ToString())}!", Colors.Red));
            // Звук объявления (vanilla event, см. Audio/NISound.cs)
            Audio.NISound.PlayMutator();
        }
    }
}
