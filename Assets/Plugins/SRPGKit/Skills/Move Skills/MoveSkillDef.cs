using UnityEngine;
using System.Collections.Generic;

public class MoveSkillDef : ActionSkillDef {
	//overridden/overridable properties
	override public MoveExecutor Executor { get { return moveExecutor; } }

	//def properties
	public bool animateTemporaryMovement=false;
	public float XYSpeed = 12;
	public float ZSpeedUp = 15;
	public float ZSpeedDown = 20;

	public bool remainMounted = true;

	//internals
	public MoveExecutor moveExecutor;

	public override void Start() {
		base.Start();
		moveExecutor = new MoveExecutor();
		Executor.lockToGrid = lockToGrid;
		Executor.character = character;
		Executor.map = map;
	}

	protected override void ResetSkill() {
		skillName = "Move";
		skillGroup = "";
		skillSorting=-1;
		
		TargetSettings ts = new TargetSettings();
		ts.targetRegion = new Region();
		ts.targetRegion.type = RegionType.Cylinder;
		ts.targetRegion.interveningSpaceType = InterveningSpaceType.Path;
		ts.targetRegion.radiusMaxF = Formula.Constant(3);
		ts.targetRegion.zUpMaxF = Formula.Constant(2);
		ts.targetRegion.zDownMaxF = Formula.Constant(3);
		ts.targetRegion.useMountingStepBonus = true;
		ts.targetRegion.canMountFriends = true;
		
		ts.effectRegion = new Region();
		ts.effectRegion.type = RegionType.Cylinder;
		ts.targetRegion.interveningSpaceType = InterveningSpaceType.Pick;
		ts.effectRegion.IsEffectRegion = true;
		ts.effectRegion.radiusMinF = Formula.Constant(0);
		ts.effectRegion.radiusMaxF = Formula.Constant(0);
		ts.effectRegion.canTargetEnemies = false;
		ts.effectRegion.canHaltAtEnemies = false;
		
		targetSettings = new TargetSettings[]{ts};
	}

	public override void Update() {
		if(!isActive) { return; }
		Executor.character = character;
		Executor.map = map;
		base.Update();
		Executor.Update();
	}

	public override void Cancel() {
		if(!isActive) { return; }
		Executor.Cancel();
		base.Cancel();
	}

	public virtual void TemporaryMove(Vector3 tc) {
		TemporaryExecutePathTo(new PathNode(tc, null, 0));
	}

	public virtual void IncrementalMove(Vector3 tc) {
		IncrementalExecutePathTo(new PathNode(tc, null, 0));
	}

	public virtual void PerformMove(Vector3 tc) {
		ExecutePathTo(new PathNode(tc, null, 0));
	}

	public virtual void TemporaryMoveToPathNode(PathNode pn, MoveExecutor.MoveFinished callback=null) {
		currentTarget.Path(pn);
		MoveExecutor me = Executor;
		me.TemporaryMoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
			scheduler.CharacterMovedTemporary(
				character,
				map.InverseTransformPointWorld(src),
				map.InverseTransformPointWorld(endNode.pos),
				pn
			);
			if(callback != null) {
				callback(src, endNode, finishedNicely);
			}
		}, 10.0f, false, remainMounted);
	}

	public virtual void IncrementalMoveToPathNode(PathNode pn, MoveExecutor.MoveFinished callback=null) {
		currentTarget.Path(pn);
		MoveExecutor me = Executor;
		me.IncrementalMoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
/*			Debug.Log("moved from "+src);*/
			scheduler.CharacterMovedIncremental(
				character,
				src,
				endNode.pos,
				pn
			);
			if(callback != null) {
				callback(src, endNode, finishedNicely);
			}
		}, 10.0f, false, remainMounted);
	}

	public virtual void PerformMoveToPathNode(PathNode pn, MoveExecutor.MoveFinished callback=null) {
		currentTarget.Path(pn);
		// Debug.Log("perform move to "+currentTarget);
		MoveExecutor me = Executor;
		// if(!(currentSettings.targetingMode == TargetingMode.Path && currentSettings.immediatelyExecuteDrawnPath)) {
		// 	Debug.Log("first, pop back to "+initialTarget.Position);
		// 	me.ImmediatelyMoveTo(new PathNode(initialTarget.Position, null, 0));
		// }
		//FIXME: really? what about chained moves?
		if(character.IsMounting && !remainMounted) {
			character.Dismount();
		}
		me.MoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
			Character c = map.OtherCharacterAt(character, endNode.pos);
			if(c != null &&
			   !character.IsMounting && !character.IsMounted &&
			   !c.IsMounted && !c.IsMounting) {
				if(character.CanMount(c) && c.IsMountableBy(character)) {
					character.Mount(c);
				} else {
					Debug.LogError("Can't mount character we're standing on!");
				}
			}
			scheduler.CharacterMoved(
				character,
				map.InverseTransformPointWorld(src),
				map.InverseTransformPointWorld(endNode.pos),
				pn
			);
			if(callback != null) {
				callback(src, endNode, finishedNicely);
			}
		}, 10.0f, false, remainMounted);
	}

	protected override PathNode[] GetValidActionTiles() {
		if(!lockToGrid) { return null; }
		Debug.Log("ct:"+currentTarget);
		return currentSettings.targetRegion.GetValidTiles(
			TargetPosition, TargetFacing,
			currentSettings.targetRegion.radiusMin, currentSettings.targetRegion.radiusMax-radiusSoFar,
			currentSettings.targetRegion.zDownMin, currentSettings.targetRegion.zDownMax,
			currentSettings.targetRegion.zUpMin, currentSettings.targetRegion.zUpMax,
			currentSettings.targetRegion.lineWidthMin, currentSettings.targetRegion.lineWidthMax,
			currentSettings.targetRegion.interveningSpaceType
		);
	}

	//N.B. for some reason, putting UpdateParameters inside of CreateOverlay -- even with
	//checks to see if the overlay already existed -- caused horrible unity crashers.

	override public void ActivateSkill() {
		Executor.character = character;
		Executor.map = map;
		Executor.lockToGrid = lockToGrid;
		Executor.animateTemporaryMovement = animateTemporaryMovement;
		Executor.XYSpeed = XYSpeed;
		Executor.ZSpeedUp = ZSpeedUp;
		Executor.ZSpeedDown = ZSpeedDown;
		Executor.Activate();

		base.ActivateSkill();
	}

	override public void DeactivateSkill() {
		if(!isActive) { return; }
		Executor.Deactivate();
		base.DeactivateSkill();
	}

	public override void ConfirmationDenied() {
		currentTarget.character = null;
	}

	protected override void ResetActionSkill() {
		overlayColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
		highlightColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
	}

	override protected void TemporaryExecutePathTo(PathNode p) {
		if(Executor.IsMoving) { return; }
		currentTarget.Path(p);
		// Debug.Log("temp path to "+p);
		TemporaryMoveToPathNode(p, (src, endNode, finishedNicely) => {
			TentativePick(endNode);
		});
	}

	override protected void IncrementalExecutePathTo(PathNode p) {
		if(Executor.IsMoving) { return; }
		currentTarget.Path(p);
		// Debug.Log("inc path to "+p);
		IncrementalMoveToPathNode(p, (src, endNode, finishedNicely) => {
			TentativePick(endNode);
		});
	}

	override protected void ExecutePathTo(PathNode p) {
		currentTarget.Path(p);
		// Debug.Log("ex path to "+p);
		PerformMoveToPathNode(p, (src, endNode, finishedNicely) => {
			Pick(endNode);
		});
	}

	override protected void LastTargetPushed() {
//		PerformMoveToPathNode(currentTarget.path, (src, endNode, finishedNicely) => {
			base.LastTargetPushed();
//		});
	}


}
