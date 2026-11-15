using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Named interaction regions traced onto the ship artwork. State belongs to one flight.</summary>
public sealed class ShipEquipment
{
    public sealed class Element
    {
        public readonly string id,title,kind,station;
        public readonly int deck;
        public readonly float pixelX;
        public Element(string id,string title,string kind,string station,int deck,float pixelX)
        {this.id=id;this.title=title;this.kind=kind;this.station=station;this.deck=deck;this.pixelX=pixelX;}
        public Vector2 Position => new Vector2(ShipLayout.ToWorldX(pixelX),ShipLayout.FloorY(deck,ShipLayout.ToWorldX(pixelX)));
        public bool Contains(Vector2 point)=>Mathf.Abs(point.x-Position.x)<1.15f && point.y>=Position.y && point.y<=Position.y+2.1f;
    }
    public static readonly Element[] Elements={
        new Element("navigation","Flight / navigation console","console","bridge",2,300),
        new Element("bridge","Bridge telemetry console","console","bridge",2,480),
        new Element("bridge-door","Bridge bulkhead","door","",2,720),
        new Element("communications","Earth communications terminal","comms","communications",2,930),
        new Element("comms-door","Communications bulkhead","door","",2,1150),
        new Element("systems","Power / systems control","power","systems",2,1400),
        new Element("systems-door","Propulsion safety bulkhead","door","",2,1600),
        new Element("propulsion","Propulsion remote monitor","console","propulsion",2,1640),
        new Element("docking","Docking hatch / pressure panel","docking","docking",1,260),
        new Element("bunk-a","Crew bunk A","rest","quarters",1,450),
        new Element("bunk-b","Crew bunk B","rest","quarters",1,590),
        new Element("bunk-c","Crew bunk C","rest","quarters",1,730),
        new Element("quarters-door","Crew quarters bulkhead","door","",1,850),
        new Element("mess","Recreation / crew status console","console","mess",1,1210),
        new Element("food","Food / water dispenser","supplies","mess",1,1410),
        new Element("medical-cabinet","First-aid cabinet","medical","medbay",1,1515),
        new Element("medbay","Crew health monitor / bed","medical","medbay",1,1610),
        new Element("laboratory","Laboratory science terminal","science","laboratory",1,1780),
        new Element("samples","Sample / growth chamber","science","laboratory",1,1940),
        new Element("cargo","Cargo inventory","supplies","storage",0,350),
        new Element("water","Water / food shielding stores","shielding","storage",0,470),
        new Element("storage-door","Storage bulkhead","door","",0,610),
        new Element("engineering","Engineering diagnostic console","console","engineering",0,755),
        new Element("engine","Engine / valve-bank service panel","console","engineering",0,880),
        new Element("shelter","Central shelter shielding controls","shelter","shelter",0,1120),
        new Element("oxygen","Oxygen tank regulator","life-support","life-support",0,1320),
        new Element("scrubber","CO2 scrubber / cabin monitor","life-support","life-support",0,1460),
        new Element("life-support-door","Life support bulkhead","door","",0,1620),
        new Element("waste","Waste isolation controls","waste","waste",0,1750),
        new Element("recycler","Water recovery unit","waste","waste",0,1880),
        new Element("ladder-lower","Lower-deck ladder","ladder","",0,1030),
        new Element("ladder-middle","Middle-deck ladder","ladder","",1,1030),
        new Element("ladder-upper","Upper-deck ladder","ladder","",2,1030)
    };
    private readonly ShipMission owner;
    private readonly HashSet<string> used=new HashSet<string>();
    private readonly HashSet<string> closedDoors=new HashSet<string>();
    private readonly Dictionary<string,string> records=new Dictionary<string,string>();
    public bool ShieldingInstalled {get;private set;}
    public bool ShelterSealed {get;private set;}
    public bool NonEssentialPower {get;private set;}=true;
    public ShipEquipment(ShipMission owner){this.owner=owner;}
    public Element Find(string id)=>Array.Find(Elements,x=>x.id==id);
    public bool IsDoorClosed(string id)=>closedDoors.Contains(id);
    public string Status(Element e)
    {
        string state=e.kind=="door" ? (IsDoorClosed(e.id)?"CLOSED — walking passage blocked":"OPEN — walking passage clear") :
            e.kind=="shelter" || e.kind=="shielding" ? "Water / food shielding: "+(ShieldingInstalled?"INSTALLED":"AVAILABLE")+"\nShelter seal: "+(ShelterSealed?"CLOSED":"OPEN") :
            e.kind=="power" ? "Life support / guidance: ONLINE\nNon-essential electronics: "+(NonEssentialPower?"ON":"OFF") :
            e.kind=="medical" || e.kind=="rest" ? "Crew vitals: "+Mathf.CeilToInt(owner.Health.Current)+" / "+owner.Health.Maximum+" HP\n"+(used.Contains(e.id)?"Recovery supply already used this flight.":"Recovery supply available.") :
            e.kind=="ladder" ? "W / S: climb. A / D: step off beside a floor." :
            e.kind=="life-support" ? "Oxygen / airflow: NOMINAL\nCabin radiation: "+Mathf.RoundToInt(owner.RadiationLevel)+"%" :
            "System online. "+owner.PhaseLabel;
        return e.title+"\n\n"+state+(records.TryGetValue(e.id,out var record)?"\n\n"+record:"");
    }
    public string[] Actions(Element e)
    {
        switch(e.kind){
            case "door":return new[]{IsDoorClosed(e.id)?"OPEN BULKHEAD":"CLOSE BULKHEAD","VERIFY DOOR INTERLOCK"};
            case "shelter":return new[]{"INSTALL WATER / FOOD SHIELDING",ShelterSealed?"OPEN SHELTER SEAL":"CLOSE SHELTER SEAL"};
            case "shielding":return new[]{"ARRANGE WATER / FOOD SHIELDING","CHECK SHIELDING INVENTORY"};
            case "power":return new[]{NonEssentialPower?"SHUT DOWN NON-ESSENTIAL LOADS":"RESTORE NON-ESSENTIAL LOADS","VERIFY ESSENTIAL POWER"};
            case "medical":return new[]{"USE FIRST-AID SUPPLY (+20 HP)","READ CREW EXPOSURE MONITOR"};
            case "rest":return new[]{"USE RECOVERY BREAK (+5 HP)","CHECK CREW READINESS"};
            case "comms":return new[]{"TEST EARTH TELEMETRY LINK","TRANSMIT STATUS REPORT"};
            case "docking":return new[]{"VERIFY LATCH / TUNNEL PRESSURE","KEEP EXTERIOR HATCH SEALED"};
            case "supplies":return new[]{"CHECK FOOD / WATER INVENTORY","SECURE LOOSE CARGO"};
            case "science":return new[]{"RUN INSTRUMENT CALIBRATION","RECORD SAMPLE STATUS"};
            case "life-support":return new[]{"CHECK OXYGEN / AIRFLOW","VERIFY CABIN SHIELDING"};
            case "waste":return new[]{"ISOLATE WASTE LINE","CHECK WATER RECOVERY"};
            case "ladder":return new[]{e.deck==2?"GRIP LADDER DOWN":"GRIP LADDER UP","READ LADDER CONTROLS"};
            default:return new[]{"RUN EQUIPMENT DIAGNOSTIC","READ SYSTEM STATUS"};
        }
    }
    public string Operate(Element e,int action)
    {
        if(action<0 || action>1) return "Unknown action.";
        string result="System inspection recorded.";
        if(e.kind=="door" && action==0){
            if(!IsDoorClosed(e.id) && owner.IsStandingInDoor(e)) return "Step clear of the doorway before closing it.";
            if(!closedDoors.Remove(e.id))closedDoors.Add(e.id);
            result=IsDoorClosed(e.id)?"Bulkhead closed; passage blocked.":"Bulkhead opened; passage clear.";
        }
        else if(e.kind=="shelter"){
            if(action==0){ShieldingInstalled=true;result="Water / food shielding installed around the central shelter.";}
            else{ShelterSealed=!ShelterSealed;result=ShelterSealed?"Shelter seal closed. Remain here for radiation protection.":"Shelter seal opened.";}
        }
        else if(e.kind=="shielding" && action==0){ShieldingInstalled=true;result="Water / food stores arranged as central shelter shielding.";}
        else if(e.kind=="power" && action==0){NonEssentialPower=!NonEssentialPower;result="Essential guidance / life support stay online; non-essential loads "+(NonEssentialPower?"restored.":"isolated.");}
        else if((e.kind=="medical" || e.kind=="rest") && action==0){
            if(used.Contains(e.id))return "This recovery supply has already been used.";
            if(owner.Health.Current>=owner.Health.Maximum)return "Crew vitals are full; supply saved.";
            used.Add(e.id);owner.Health.Heal(e.kind=="medical"?20:5);result="Recovery supply used; crew vitals updated.";
        }
        else if(e.kind=="ladder" && action==0)result="Grip the ladder with W / S, then use A / D at a floor to step off.";
        else if(e.kind=="comms")result=action==0?"Earth telemetry link locked; packets verified.":"Status report transmitted to Mission Control.";
        else if(e.kind=="docking")result="Docking latches verified; tunnel pressure equalized. Exterior hatch remains sealed.";
        else if(e.kind=="science")result=action==0?"Instrument calibration complete.":"Sample condition recorded.";
        else if(e.kind=="waste")result=action==0?"Waste line safely isolated.":"Water recovery operating within reference limits.";
        else if(e.kind=="life-support")result=action==0?"Oxygen regulator and scrubber airflow verified.":"Cabin field measured. Use water / food shielding and the central shelter seal.";
        else if(e.kind=="supplies")result=action==0?"Food / water stores inventoried.":"Cargo restraints verified.";
        records[e.id]=result;return result;
    }
    public void ConfigureShelterStep(int index)
    {
        if(index==1)ShieldingInstalled=true;
        if(index==2)NonEssentialPower=false;
        if(index==4)ShelterSealed=true;
    }
    public float ClampMovement(int deck,float from,float requested)
    {
        foreach(var e in Elements){
            if(e.deck!=deck || e.kind!="door" || !IsDoorClosed(e.id))continue;
            float x=e.Position.x;
            if(from<x && requested>x-ShipLayout.BodyHalfWidth)requested=Mathf.Max(from,x-ShipLayout.BodyHalfWidth);
            if(from>x && requested<x+ShipLayout.BodyHalfWidth)requested=Mathf.Min(from,x+ShipLayout.BodyHalfWidth);
        }
        return requested;
    }
}
