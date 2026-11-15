void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
var catalog=UnityEngine.Resources.LoadAll<ShipMissionDefinition>("Missions").Where(x=>!x.startsQuestionCampaign).ToArray();
Check(catalog.Length==35,"Expected all 35 specification tasks");
Check(catalog.Count(x=>x.globalEvent)==4&&catalog.Count(x=>!x.globalEvent)==31,"Incorrect global/shared pool sizes");
foreach(var d in catalog){
 Check(d.steps.Length>=3&&d.objectiveType==ShipMissionObjectiveType.Procedure,"Missing procedure: "+d.missionId);
 int cap=d.missionId=="G-01"?40:d.missionId=="CMD-02"||d.missionId=="ENG-02"||d.missionId=="PIL-01"?35:d.difficulty=="Easy"?30:d.difficulty=="Medium"?60:90;
 Check(d.durationSeconds==cap,"Invalid active-time cap: "+d.missionId);
 Check(d.steps.Where(x=>x.automatic).Sum(x=>x.durationSeconds)<d.durationSeconds,"Procedure cannot finish within cap: "+d.missionId);
 Check(d.steps.All(x=>x.automatic || x.options.Length>0&&x.correctChoice>=0&&x.correctChoice<x.options.Length),"Invalid actions: "+d.missionId);
 for(int role=0;role<4;role++)Check(d.AllowsRole((CrewRole)role),"Task has a role restriction: "+d.missionId);
 Check(d.availableFromSeconds>=5&&d.availableUntilSeconds<=600,"Pre-orbit or out-of-run task: "+d.missionId);
}
var seen=new System.Collections.Generic.HashSet<string>();int two=0,three=0;
for(int seed=0;seed<2000;seed++){
 var plan=MissionRunPlan.Create(catalog,seed);var shared=plan.Where(x=>!x.definition.globalEvent).ToArray();var globals=plan.Where(x=>x.definition.globalEvent).ToArray();
 Check(plan.Length==7||plan.Length==8,"Wrong event count");Check(plan[0].definition.missionId=="G-01"&&plan[0].trigger==0&&plan.Any(x=>x.definition.missionId=="G-02"),"Mandatory checkout or radiation story beat missing");Check(shared.Length==5&&globals.Length==plan.Length-5,"Wrong pool counts");
 Check(shared.Count(x=>x.definition.difficulty=="Easy")==2&&shared.Count(x=>x.definition.difficulty=="Medium")==2&&shared.Count(x=>x.definition.difficulty=="Hard")==1,"Wrong difficulty mix");
 Check(shared.Select(x=>MissionRunPlan.PhaseAt(x.trigger)).Distinct().Count()>=3,"Insufficient phase diversity");
 Check(plan.Select(x=>x.definition.missionId).Distinct().Count()==plan.Length,"Duplicate events");
 Check(plan.All(x=>(x.definition.missionId==MissionRunPlan.CheckoutId?x.trigger==0:x.trigger>=x.definition.availableFromSeconds&&x.trigger<=x.definition.availableUntilSeconds)),"Trigger outside allowed window");
 Check(plan.Sum(x=>x.definition.durationSeconds)<=480,"Run exceeds maximum task budget");
 float cursor=0;foreach(var e in plan){cursor=UnityEngine.Mathf.Max(cursor,e.trigger)+MissionRunPlan.EstimatedWork(e.definition);seen.Add(e.definition.missionId);}Check(cursor<=600,"Generated schedule has insufficient expected completion time");
 var repeat=MissionRunPlan.Create(catalog,seed);Check(plan.Zip(repeat,(a,b)=>a.definition==b.definition&&a.trigger==b.trigger).All(x=>x),"Seed is not reproducible");
 if(globals.Length==2)two++;else three++;
}
Check(seen.Count==35,"Some tasks cannot be selected: "+string.Join(",",catalog.Where(x=>!seen.Contains(x.missionId)).Select(x=>x.missionId)));
Check(two>0&&three>0,"Global-event count does not vary");
return "PASS: all 35 procedures, all roles, caps, action data; 2000 reproducible random runs with 5 shared + 2/3 globals, 2E/2M/1H, at least 3 phases, unique events, valid trigger windows and feasible expected run budgets; every task selected including late reentry";
