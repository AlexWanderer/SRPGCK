using UnityEngine;

//for each waypoint
//target settings:
[System.Serializable]
public class TargetSettings {
	//editor only
	public bool showInEditor=true;

	public SkillDef owner;
	public SkillDef Owner {
		get { return owner; }
		set {
			owner = value;
			if(targetRegion == null) { 
				targetRegion = new Region();
			}
			if(effectRegion == null) { 
				effectRegion = new Region(); 
			}
			targetRegion.Owner = owner;
			effectRegion.Owner = owner;
			effectRegion.IsEffectRegion = true;
		}
	}
	public Formulae fdb { get {
		if(owner != null) { return owner.fdb; }
		return Formulae.DefaultFormulae;
	} }

	public TargetingMode targetingMode = TargetingMode.Pick;
	//tile generation region (line/range/cone/etc)
	public Region targetRegion, effectRegion;
	public bool displayUnimpededTargetRegion=false;

	public bool doNotMoveChain=false;
	public bool allowsCharacterTargeting=false;
	public float newNodeThreshold=0.05f;
	public bool immediatelyExecuteDrawnPath=false;
	public Formula rotationSpeedXYF;
	public float rotationSpeedXY { get {
		return rotationSpeedXYF.GetValue(fdb, owner);
	} }

	public bool IsPickOrPath { get {
		return
			targetingMode == TargetingMode.Pick ||
			targetingMode == TargetingMode.SelectRegion ||
			targetingMode == TargetingMode.Path;
	} }

	public bool ShouldDrawPath { get {
		return targetingMode == TargetingMode.Path;
	} }

	public bool DeferPathRegistration { get {
		return !(ShouldDrawPath || immediatelyExecuteDrawnPath);
	} }
}
