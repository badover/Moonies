void Check(bool ok,string text){if(!ok)throw new System.Exception(text);}
UnityEngine.Application.runInBackground=true;
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();
var checkout=UnityEngine.Resources.Load<ShipMissionDefinition>("Missions/G01_LEO_Check");
ShipMission mission=null;
void Set(string field,object value)=>typeof(ShipMission).GetField(field,flags).SetValue(mission,value);
void At(int deck,UnityEngine.Vector2 point){var crew=mission.GetComponentInChildren<CrewCharacter>();crew.transform.position=(UnityEngine.Vector3)point+UnityEngine.Vector3.back;Set("deck",deck);Set("activeLadder",-1);}
void AtElement(ShipEquipment.Element e){float x=ShipLayout.ClampX(e.deck,e.Position.x);At(e.deck,new UnityEngine.Vector2(x,ShipLayout.FloorY(e.deck,x)));}
void FinishCheckout(){Check(mission.StartMission(checkout),"Mandatory checkout cannot start");while(mission.ActiveMission!=null){var step=mission.CurrentStep;if(step.automatic)mission.SendMessage("UpdateActiveMission",step.durationSeconds);else Check(mission.ChooseProcedureOption(step.correctChoice),"Checkout action rejected");}Check(mission.CheckoutComplete,"Checkout completion does not unlock chain");}
async System.Threading.Tasks.Task NewFlight(int role){if(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(80);}menu.SendMessage("Select",role);menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();mission.enabled=false;}
var results=new System.Collections.Generic.List<string>();
for(int role=0;role<4;role++){
 await NewFlight(role);
 Check(mission.RunPlan[0].definition==checkout&&mission.RunPlan[0].trigger==0,"First mission missing from random draw");
 Check(mission.GetMissionStatus(checkout)==ShipMissionStatus.Available&&mission.RunPlan.Skip(1).All(x=>mission.GetMissionStatus(x.definition)==ShipMissionStatus.Unavailable),"Chain runs before first checkout");
 Check(!mission.SkipMission(checkout),"First mission can be skipped");
 Check(mission.MissionSeconds==0,"Flight phase clock advances before checkout");
 Check(mission.StartMission(checkout),"Cannot start first checkout");mission.SendMessage("UpdateActiveMission",31f);
 Check(!mission.CheckoutComplete&&mission.GetMissionStatus(checkout)==ShipMissionStatus.Available&&mission.Health.Current==95,"Failed checkout cannot be retried or unlocks the chain");mission.Health.Heal(5);
 FinishCheckout();
 var releases=new System.Collections.Generic.List<string>();mission.MissionAvailable+=d=>releases.Add(d.missionId);
 var early=mission.RunPlan.First(x=>!x.definition.globalEvent);Set("missionClock",early.trigger+.01f);mission.SendMessage("UpdateFlightEvents");
 Check(releases.Contains(early.definition.missionId),"First post-checkout task has no event announcement");int announced=releases.Count;mission.SendMessage("UpdateFlightEvents");Check(releases.Count==announced,"Release announced repeatedly");
 Check(!mission.StartMission(early.definition),"Equipment task starts remotely");mission.TrackStation(early.definition);Check(mission.TrackedEquipment!=null,"No world equipment waypoint");
 At(early.definition.deck,early.definition.WorldPosition);Check(mission.StartMission(early.definition),"Cannot start task at its equipment");mission.SendMessage("UpdateActiveMission",early.definition.durationSeconds+1);mission.Health.Heal(100);
 var storm=mission.RunPlan.Single(x=>x.definition.missionId=="G-02");Set("missionClock",storm.trigger+.01f);At(1,new UnityEngine.Vector2(-9,ShipLayout.FloorY(1,-9)));mission.SendMessage("UpdateFlightEvents");
 Check(mission.ActiveMission==null&&mission.RadiationLevel==35&&mission.CabinEventSecondsRemaining==120,"Cabin radiation requires manually accepting an event");
 float hp=mission.Health.Current;mission.SendMessage("UpdateCabinRadiation",10f);Check(UnityEngine.Mathf.Abs(mission.Health.Current-(hp-7))<.001f,"Cabin field is cosmetic; actual HP did not drop");
 mission.SetPaused(true);hp=mission.Health.Current;float remaining=mission.CabinEventSecondsRemaining;mission.SendMessage("UpdateCabinRadiation",10f);Check(mission.Health.Current==hp&&mission.CabinEventSecondsRemaining==remaining,"Pause does not freeze exposure");mission.SetPaused(false);
 var shelter=mission.Equipment.Find("shelter");AtElement(shelter);Check(mission.OperateEquipment("shelter",0)&&mission.OperateEquipment("shelter",1),"Cannot configure physical shelter");
 Check(mission.IsRadiationProtected&&mission.RadiationDamagePerSecond==0,"Shielded / sealed shelter fails to protect");hp=mission.Health.Current;mission.SendMessage("UpdateCabinRadiation",10f);Check(mission.Health.Current==hp,"HP drains inside protected shelter");
 At(0,new UnityEngine.Vector2(8,ShipLayout.FloorY(0,8)));hp=mission.Health.Current;mission.SendMessage("UpdateCabinRadiation",5f);Check(mission.Health.Current<hp,"Leaving shelter does not restore exposure");
 At(1,new UnityEngine.Vector2(100,100));hp=mission.Health.Current;mission.SendMessage("UpdateCabinRadiation",5f);Check(mission.Health.Current==hp,"Cabin-only radiation damages outside the ship");
 At(1,new UnityEngine.Vector2(-9,ShipLayout.FloorY(1,-9)));mission.Health.Heal(100);mission.SendMessage("UpdateCabinRadiation",mission.CabinEventSecondsRemaining+10);Check(mission.RadiationLevel==0&&mission.RadiationDamagePerSecond==0,"Cleared event still irradiates cabin");
 hp=mission.Health.Current;mission.SendMessage("UpdateCabinRadiation",10f);Check(mission.Health.Current==hp,"Exposure damage persists after clear");
 results.Add(((CrewRole)role)+": mandatory first scan, gated event chain, release announcements, physical station access, automatic cabin hazard, continuous HP damage, pause, sheltered/outside/clear behavior PASS");
}
await NewFlight(2);
int opened=0;mission.EquipmentInteractionRequested+=_=>opened++;
foreach(var e in ShipEquipment.Elements){AtElement(e);Check(mission.TryInteractAtPoint(e.Position+UnityEngine.Vector2.up),"World-click region missing: "+e.id);Check(mission.OperateEquipment(e.id,1),"Equipment action unavailable: "+e.id);menu.SendMessage("CloseEquipment");}
Check(opened==ShipEquipment.Elements.Length,"Not every mapped element opens an interaction panel");
var door=mission.Equipment.Find("bridge-door");At(door.deck,door.Position+UnityEngine.Vector2.left);Check(mission.OperateEquipment(door.id,0)&&mission.Equipment.IsDoorClosed(door.id),"Door action does not close it");mission.MoveCrew(1,0,1);var player=mission.GetComponentInChildren<CrewCharacter>();Check(player.transform.position.x<=door.Position.x-ShipLayout.BodyHalfWidth+.001f,"Closed door does not block walking");Check(mission.OperateEquipment(door.id,0)&&!mission.Equipment.IsDoorClosed(door.id),"Cannot reopen closed door");mission.MoveCrew(1,0,1);Check(player.transform.position.x>door.Position.x,"Open door still blocks walking");
var medical=mission.Equipment.Find("medical-cabinet");AtElement(medical);mission.Health.TakeDamage(40);Check(mission.OperateEquipment(medical.id,0)&&mission.Health.Current==80,"First aid does not restore HP");mission.OperateEquipment(medical.id,0);Check(mission.Health.Current==80,"First aid can be farmed repeatedly");
FinishCheckout();var radiation=mission.RunPlan.Single(x=>x.definition.missionId=="G-02");Set("missionClock",radiation.trigger+.01f);mission.SendMessage("UpdateFlightEvents");AtElement(mission.Equipment.Find("communications"));mission.InteractEquipment("communications");Check(!mission.IsPaused&&mission.IsInterfaceOpen,"Equipment dialog pauses hazard");
float prior=mission.Health.Current;mission.enabled=true;await System.Threading.Tasks.Task.Delay(500);mission.enabled=false;Check(mission.Health.Current<prior,"Real frames do not damage HP while equipment panel is open");menu.SendMessage("CloseEquipment");
mission.Health.TakeDamage(mission.Health.Current-.1f);mission.SendMessage("UpdateCabinRadiation",1f);Check(mission.Health.Current==0&&mission.IsPaused,"Radiation cannot kill or stop the game");menu.SendMessage("TogglePause");Check(mission.IsPaused,"Escape bypasses radiation death");
await NewFlight(2);Check(mission.Health.Current==100&&mission.RadiationLevel==0&&!mission.Equipment.ShieldingInstalled&&!mission.Equipment.ShelterSealed,"New flight retains damage or shelter state");
results.Add("All "+ShipEquipment.Elements.Length+" world-click elements and panels, door collisions/reopening, limited first aid, real-frame damage during dialogs, radiation death and replay reset PASS");
menu.SendMessage("ReturnToMenu");return results;
