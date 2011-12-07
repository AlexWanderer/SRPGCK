using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ActionIO : ITilePickerOwner {
	[System.NonSerialized]
	public AttackSkill owner;
	
	[HideInInspector]
	public bool isActive;
	
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	
	//confirmation
	
	public bool requireConfirmation = true;
	public bool RequireConfirmation { get { return requireConfirmation; } }
	
	public bool AwaitingConfirmation {
		get { return tilePicker.AwaitingConfirmation; }
		set { tilePicker.AwaitingConfirmation = value; }
	}
	
	//grid
	
	public GridOverlay overlay;
	public float indicatorCycleLength=1.0f;
	
	//pick
	
	TilePicker tilePicker;
	
	public PathNode[] targetTiles;
	
	public virtual void Activate () {
		isActive = true;
		//pick
		tilePicker = new TilePicker();
		tilePicker.owner = this;
	}
	
	public virtual void Update () {
		if(owner.character == null || !owner.character.isActive) { return; }
		if(!isActive) { return; }
		if(!owner.arbiter.IsLocalPlayer(owner.character.EffectiveTeamID)) {
			return;
		}
		if(GUIUtility.hotControl != 0) { return; }
		tilePicker.supportKeyboard      = supportKeyboard;
		tilePicker.supportMouse         = supportMouse;
		tilePicker.requireConfirmation  = requireConfirmation;
		tilePicker.indicatorCycleLength = indicatorCycleLength;
		tilePicker.Update();
	}
	
	public virtual void Deactivate() {
		isActive = false;
		
		//grid
		
		overlay = null;
		tilePicker.Clear();
		if(owner.map.IsShowingOverlay(owner.skillName, owner.character.gameObject.GetInstanceID())) {
			owner.map.RemoveOverlay(owner.skillName, owner.character.gameObject.GetInstanceID());
		}	
	}
	
	public Map Map { get { return owner.map; } }
	
	public void CancelPick(TilePicker tp) {
		owner.Cancel();
	}
	
	public Vector3 IndicatorPosition {
		get { return tilePicker.IndicatorPosition; }
	}	
	
	virtual public void PresentMoves() {
		tilePicker.requireConfirmation = requireConfirmation;
		tilePicker.map = owner.map;
		ActionStrategy strat = owner.strategy as ActionStrategy;
		PathNode[] destinations = strat.GetValidActions();
/*		MoveExecutor me = owner.executor;*/
		Vector3 charPos = owner.character.TilePosition;
		overlay = owner.map.PresentGridOverlay(
			owner.skillName, owner.character.gameObject.GetInstanceID(), 
			new Color(0.6f, 0.3f, 0.2f, 0.7f),
			new Color(0.9f, 0.6f, 0.4f, 0.85f),
			destinations
		);
		tilePicker.PresentOverlay(overlay, charPos);
	}
	
	public void FocusOnPoint(Vector3 pos) {
		tilePicker.FocusOnPoint(pos);
	}

	public void TentativePick(TilePicker tp, Vector3 p) {
		targetTiles = owner.strategy.GetTargetedTiles(p);
		overlay.SetSelectedPoints(owner.map.CoalesceTiles(targetTiles));
		//TODO: show preview indicator until cancelled
	}	
	
	public void TentativePick(TilePicker tp, PathNode pn) {
		targetTiles = owner.strategy.GetTargetedTiles(pn.pos);
		overlay.SetSelectedPoints(owner.map.CoalesceTiles(targetTiles));
		//TODO: show preview indicator until cancelled
	}
	
	public void CancelEffectPreview() {

	}
	
	public void Pick(TilePicker tp, Vector3 p) {
		targetTiles = owner.strategy.GetTargetedTiles(p);
		owner.ApplySkill();
	}
	
	public void Pick(TilePicker tp, PathNode pn) {
		targetTiles = owner.strategy.GetTargetedTiles(pn.pos);
		owner.ApplySkill();
	}
}