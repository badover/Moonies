void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
if(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(100);}
var results=new System.Collections.Generic.List<string>();
for(int role=0;role<4;role++){
 menu.SendMessage("Select",role);menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);
 var mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();var health=mission.Health;
 var fill=(UnityEngine.UI.Image)typeof(CrewMenu).GetField("healthFill",flags).GetValue(menu);var text=(UnityEngine.UI.Text)typeof(CrewMenu).GetField("healthValue",flags).GetValue(menu);
 var panel=UnityEngine.GameObject.Find("Health HUD").GetComponent<UnityEngine.RectTransform>();
 Check(health!=null&&health.Current==100&&health.Maximum==100&&fill.fillAmount==1&&text.text=="100%","Initial health incorrect");
 Check(panel.sizeDelta.x<=220&&panel.sizeDelta.y<=48,"Health panel too large");Check(!fill.raycastTarget&&fill.sprite!=null,"Health bar blocks input or lacks fill sprite");
 int changes=0;health.Changed+=_=>changes++;
 health.TakeDamage(25);Check(health.Current==75&&UnityEngine.Mathf.Abs(fill.fillAmount-.75f)<.001&&text.text=="75%","Damage does not update HUD");
 UnityEngine.Canvas.ForceUpdateCanvases();var mesh=fill.canvasRenderer.GetMesh();Check(mesh.vertexCount>0&&UnityEngine.Mathf.Abs(mesh.bounds.size.x/fill.rectTransform.rect.width-.75f)<.01f,"Fill geometry is not 75 percent");
 health.TakeDamage(35);Check(health.Current==40&&fill.color.r>fill.color.g,"Mid-health amber absent");health.TakeDamage(30);Check(health.Current==10&&fill.color.r>fill.color.g*2,"Low-health red absent");
 health.Heal(30);Check(health.Current==40&&text.text=="40%","Healing does not update HUD");health.Heal(1000);Check(health.Current==100&&fill.fillAmount==1,"Overhealing not capped");
 int before=changes;health.TakeDamage(-10);health.Heal(-10);health.TakeDamage(float.NaN);health.Heal(float.PositiveInfinity);Check(health.Current==100&&changes==before,"Invalid input altered health");
 health.TakeDamage(1000);Check(health.Current==0&&fill.fillAmount==0&&text.text=="0%","Damage below zero or empty bar failed");before=changes;health.TakeDamage(10);Check(changes==before,"No-op damage emits changes");health.Heal(72);Check(health.Current==72&&text.text=="72%","Recovery from zero failed");
 results.Add(((CrewRole)role)+": full health, damage, healing, clamps, invalid values, HUD geometry/colors and compact size PASS");
 menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(100);
}
return results;
