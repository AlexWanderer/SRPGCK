using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaitSkillDef : ActionSkillDef {
 	[System.NonSerialized]
	public Transform instantiatedWaitArrows;
	public enum Arrow {
		YP,
		XP,
		YN,
		XN
	};
	public Arrow currentArrow = Arrow.YP;
	public GameObject waitArrows;

	protected override void ResetActionSkill() {
		if(waitArrows == null) {
			waitArrows = Resources.LoadAssetAtPath("Assets/SRPGKit/Prefabs/Wait Arrows.prefab", typeof(GameObject)) as GameObject;
		}
		TargetSettings ts = new TargetSettings();
		targetSettings = new TargetSettings[]{ts};
		overlayColor = Color.clear;
		highlightColor = Color.clear;
		ts.targetingMode = TargetingMode.Cardinal;
		ts.targetRegion = new Region();
		ts.targetRegion.type = RegionType.Self;
		ts.targetRegion.interveningSpaceType = InterveningSpaceType.Pick;
		ts.effectRegion = new Region();
		ts.effectRegion.type = RegionType.Self;
		ts.effectRegion.interveningSpaceType = InterveningSpaceType.Pick;
		StatEffect facingEffect = new StatEffect();
		facingEffect.effectType = StatEffectType.ChangeFacing;
		facingEffect.target = StatEffectTarget.Applier;
		facingEffect.triggerF = Formula.True();
		facingEffect.value = Formula.Lookup("arg.angle.xy", LookupType.SkillParam);
		StatEffect endTurnEffect = new StatEffect();
		endTurnEffect.effectType = StatEffectType.EndTurn;
		endTurnEffect.target = StatEffectTarget.Applier;
		endTurnEffect.triggerF = Formula.True();
		applicationEffects = new StatEffectGroup{effects=new StatEffect[]{
			facingEffect, endTurnEffect
		}};
	}

	protected override void ResetSkill() {
		skillName = "Wait";
		skillSorting = 100000;
		isEnabledF = Formula.True();
		reallyDefined = true;
	}

	public override void ActivateSkill() {
		if(isActive) { return; }
		base.ActivateSkill();
		instantiatedWaitArrows = (Object.Instantiate(waitArrows) as GameObject).transform;
		instantiatedWaitArrows.parent = map.transform;
		instantiatedWaitArrows.position = character.transform.position;
		MoveToLayer(instantiatedWaitArrows, LayerMask.NameToLayer("Wait Arrows"));
		WaitAtArrow(ArrowForFacing(character.Facing));
	}
	public override void DeactivateSkill() {
		if(!isActive) { return; }
		instantiatedWaitArrows.Find("XP").localScale = new Vector3(1,1,1);
		instantiatedWaitArrows.Find("YP").localScale = new Vector3(1,1,1);
		instantiatedWaitArrows.Find("XN").localScale = new Vector3(1,1,1);
		instantiatedWaitArrows.Find("YN").localScale = new Vector3(1,1,1);
		Object.Destroy(instantiatedWaitArrows.gameObject);
		instantiatedWaitArrows = null;
		base.DeactivateSkill();
	}
	protected override void UpdateTarget() {
		base.UpdateTarget();
		if(!isActive) { return; }
		//look for clicks on our four arrows
		if(supportMouse &&
		   Input.GetMouseButton(0) &&
		   (!awaitingConfirmation || !RequireConfirmation)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitInfo;
			//irksome that I have to bitshift the layer here, but whatever
			bool anyHit = Physics.Raycast(r, out hitInfo, Mathf.Infinity, 1<<instantiatedWaitArrows.gameObject.layer);
			Transform hitArrow = hitInfo.transform;
			Arrow hitArrowValue = Arrow.XP;
			if(anyHit) {
				if(hitArrow == instantiatedWaitArrows.Find("XP")) {
					hitArrowValue = Arrow.XP;
				} else if(hitArrow == instantiatedWaitArrows.Find("YP")) {
					hitArrowValue = Arrow.YP;
				} else if(hitArrow == instantiatedWaitArrows.Find("XN")) {
					hitArrowValue = Arrow.XN;
				} else if(hitArrow == instantiatedWaitArrows.Find("YN")) {
					hitArrowValue = Arrow.YN;
				} else {
					anyHit = false;
				}
			}
			if(anyHit) {
				WaitAtArrow(hitArrowValue);
				if(Input.GetMouseButtonDown(0) && Time.time-firstClickTime < doubleClickThreshold) {
					firstClickTime = -1;
					if(anyHit) {
						if(!RequireConfirmation) {
				    	WaitAtArrow(hitArrowValue);
							awaitingConfirmation = false;
							PickFacing(character.Facing);
						} else {
				    	WaitAtArrow(hitArrowValue);
							awaitingConfirmation = true;
						}
					}
				} else {
					if(Input.GetMouseButtonDown(0)) {
						firstClickTime = Time.time;
					}
				}
			}
		}
		WaitAtArrow(ArrowForFacing(character.Facing));
	}
	public Arrow ArrowForFacing(float f) {
		LockedFacing l = SRPGUtil.LockFacing(f, FacingLock.Cardinal, map.transform.eulerAngles.y);
		switch(l) {
			case LockedFacing.XP:
				return Arrow.XP;
			case LockedFacing.YP:
				return Arrow.YP;
			case LockedFacing.XN:
				return Arrow.XN;
			case LockedFacing.YN:
				return Arrow.YN;
			default:
				Debug.LogError("Bad locked facing "+l+" from "+f);
				return (Arrow)(-1);
		}
	}

	protected Transform currentArrowTransform { get {
		if(instantiatedWaitArrows == null) { return null; }
		switch(currentArrow) {
			case Arrow.XP: return instantiatedWaitArrows.Find("XP");
			case Arrow.YP: return instantiatedWaitArrows.Find("YP");
			case Arrow.XN: return instantiatedWaitArrows.Find("XN");
			case Arrow.YN: return instantiatedWaitArrows.Find("YN");
		}
		return null;
	} }

	public void WaitAtArrow(Arrow a) {
		const float TAU = 360.0f;
		currentArrowTransform.localScale = new Vector3(1,1,1);
		currentArrow = ArrowForFacing(character.Facing);
		currentArrowTransform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		float dir;
		switch(a) {
			case Arrow.XP: dir = 0*TAU/4; break;
			case Arrow.YP: dir = 1*TAU/4; break;
			case Arrow.XN: dir = 2*TAU/4; break;
			case Arrow.YN: dir = 3*TAU/4; break;
			default: Debug.LogError("Not my arrow!"); return;
		}
		TentativePickFacing(dir);
		FaceDirection(dir);
	}

	void MoveToLayer(Transform root, int layer) {
	  root.gameObject.layer = layer;
	  foreach(Transform child in root) {
	    MoveToLayer(child, layer);
		}
	}


}
