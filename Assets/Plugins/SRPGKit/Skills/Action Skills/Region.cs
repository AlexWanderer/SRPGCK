using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum RegionType {
	Cylinder,
	Sphere,
	Line,
	LineMove,
	//scans line of movement; can cross walls/enemies, can glide, radius, direction, z up max, z down max, width is always 1
	//somehow queryable for distance remaining and amount dropped (map can be asked about characters collided using normal means)
	//intervening space type is always LineMove, but LineMove is not allowed for other region types.
	Cone,
	Self,
	NWay, //retries subregions N times around circle, evenly spaced, with a given angle offset from start.
	Compound, //merges subregions based on their types, ignoring their intervening space types.
};

public enum InterveningSpaceType {
	Pick, //pick anywhere in 3d space
	Path, //walkable path
	Line, //straight line from source, possibly blocked by walls or enemies
	Arc,   //arced line from source, possibly blocked by walls or enemies
	LineMove //N.B. iff region type is LineMove
};

//For linemove type
public enum StuckPrevention {
	None,
	StopBefore,
	KeepGoing
}

[System.Serializable]
public class Region {
	//editor only
	public bool editorShowContents=false;

	protected SkillDef owner;
	public SkillDef Owner {
		get { return owner; }
		set {
			owner = value;
			if(regions == null) {
				return;
			}
			for(int i = 0; i < regions.Length; i++) {
				if(regions[i] == null) {
					regions[i] = new Region();
				}
				regions[i].Owner = owner;
			}
		}
	}
	public Formulae fdb { get {
		if(owner != null) { return owner.fdb; }
		return Formulae.DefaultFormulae;
	} }

	public RegionType type=RegionType.Cylinder;

	public InterveningSpaceType interveningSpaceType=InterveningSpaceType.Pick;

	public bool useArcRangeBonus=false;

	//WARNING: region generators will avoid using DZ checks if this flag is not set.
	//now, these will get filtered out by the final range filter at the end of getvalidnodes,
	//but just keep it in mind for certain fancy intervening-space generators
	//this flag will have no effect on the Pick space type or the Line or Cone region types.
	public bool useAbsoluteDZ=true;

	//these mean the same for pathable and non-pathable regions
	//they don't apply to self or predicate.
	public bool _canCrossWalls=true;
	public bool canCrossWalls {
		get { return _canCrossWalls; }
		set {
			_canCrossWalls = value;
			if(regions == null || interveningSpaceType == InterveningSpaceType.Pick) { return; }
			for(int i = 0; i < regions.Length; i++) {
				if(regions[i] == null) {
					regions[i] = new Region();
				}
				regions[i].canCrossWalls = _canCrossWalls;
			}
		}
	}
	//means "can cross characters" for line move.
	public bool _canCrossEnemies=true;
	public bool canCrossEnemies {
		get { return _canCrossEnemies; }
		set {
			_canCrossEnemies = value;
			if(regions == null || interveningSpaceType == InterveningSpaceType.Pick) { return; }
			for(int i = 0; i < regions.Length; i++) {
				if(regions[i] == null) {
					regions[i] = new Region();
				}
				regions[i].canCrossEnemies = _canCrossEnemies;
			}
		}
	}
	//turn this off for move skills!
	//it only has meaning if canCrossEnemies is false.
	//basically, it's the difference between:
	//ending ON an enemy (as an attack would); and
	//ending BEFORE an enemy (as a move would)
	public bool _canHaltAtEnemies=true;
	public bool canHaltAtEnemies {
		get { return _canHaltAtEnemies; }
		set {
			_canHaltAtEnemies = value;
			if(regions == null) { return; }
			for(int i = 0; i < regions.Length; i++) {
				if(regions[i] == null) {
					regions[i] = new Region();
				}
				regions[i].canHaltAtEnemies = canHaltAtEnemies;
			}
		}
	}

	public bool _canTargetEnemies=true;
	public bool canTargetEnemies {
		get { return _canTargetEnemies; }
		set {
			_canTargetEnemies = value;
			if(regions == null) { return; }
			for(int i = 0; i < regions.Length; i++) {
				if(regions[i] == null) {
					regions[i] = new Region();
				}
				regions[i].canTargetEnemies = canTargetEnemies;
			}
		}
	}

	public bool _canTargetFriends=true;
	public bool canTargetFriends {
		get { return _canTargetFriends; }
		set {
			_canTargetFriends = value;
			if(regions == null) { return; }
			for(int i = 0; i < regions.Length; i++) {
				if(regions[i] == null) {
					regions[i] = new Region();
				}
				regions[i].canTargetFriends = canTargetFriends;
			}
		}
	}

	public bool _canTargetSelf=true;
	public bool canTargetSelf {
		get { return _canTargetSelf; }
		set {
			_canTargetSelf = value;
			if(regions == null) { return; }
			for(int i = 0; i < regions.Length; i++) {
				if(regions[i] == null) {
					regions[i] = new Region();
				}
				regions[i].canTargetSelf = canTargetSelf;
			}
		}
	}

	public bool _useMountingStepBonus=false;
	public bool useMountingStepBonus {
		get { return _useMountingStepBonus; }
		set {
			_useMountingStepBonus = value;
			if(regions == null) { return; }
			for(int i = 0; i < regions.Length; i++) {
				if(regions[i] == null) {
					regions[i] = new Region();
				}
				regions[i].useMountingStepBonus = useMountingStepBonus;
			}
		}
	}

	public bool _canMountEnemies=false;
	public bool canMountEnemies {
		get { return _canMountEnemies; }
		set {
			_canMountEnemies = value;
			if(regions == null) { return; }
			for(int i = 0; i < regions.Length; i++) {
				if(regions[i] == null) {
					regions[i] = new Region();
				}
				regions[i].canMountEnemies = canMountEnemies;
			}
		}
	}

	public bool _canMountFriends=false;
	public bool canMountFriends {
		get { return _canMountFriends; }
		set {
			_canMountFriends = value;
			if(regions == null) { return; }
			for(int i = 0; i < regions.Length; i++) {
				if(regions[i] == null) {
					regions[i] = new Region();
				}
				regions[i].canMountFriends = canMountFriends;
			}
		}
	}


	//linemove only
	public StuckPrevention preventStuckInAir  =StuckPrevention.StopBefore;
	public StuckPrevention preventStuckInWalls=StuckPrevention.StopBefore;

	public bool canGlide=false;
	public bool performFall=true;
	public FacingLock facingLock;

	//these apply to cylinder/predicate, sphere, cone, and line
	public Formula radiusMinF, radiusMaxF;
	//these apply to cylinder/predicate (define), sphere (clip), line (define up/down displacements from line), line move (max only)
	public Formula zUpMinF, zUpMaxF, zDownMinF, zDownMaxF;
	//these apply to cone and line, xy applies to lineMove & nways
	public Formula xyDirectionF, zDirectionF;
	//these apply to cone
	public Formula xyArcMinF, zArcMinF;
	public Formula xyArcMaxF, zArcMaxF;
	public Formula rFwdClipMaxF;
	//these apply to line
	public Formula lineWidthMinF, lineWidthMaxF;
	//this applies to predicate, and gets these variable bindings as skill params:
	//arg.region.{...}
	//x, y, z, angle.xy, angle.z,
	//target.x, target.y, target.z, angle.between.xy, angle.between.z
	//dx, dy, dz, distance, distance.xy, mdistance, mdistance.xy
	//as a bonus, in the scope of a region lookup the skill's "current target" is the character on a given tile
	//it should return 0 (false) or non-0 (true)
	public Formula predicateF;

	//only used for compound/nway regions. subregions of a compound region may only
	//generate tiles, and may not apply their intervening space modes.
	//more complex uses of compound spaces should subclass Skill or Region.
	public Region[] regions;
	//only used for nways region
	public Formula nWaysF;

	public float radiusMin { get { return Formula.NullFormula(radiusMinF) ? 0 : radiusMinF.GetValue(fdb, owner); } }
	public float radiusMax { get { return Formula.NullFormula(radiusMaxF) ? 0 : radiusMaxF.GetValue(fdb, owner); } }
	public float zUpMin { get { return Formula.NullFormula(zUpMinF) ? 0 : zUpMinF.GetValue(fdb, owner); } }
	public float zUpMax { get { return Formula.NullFormula(zUpMaxF) ? 0 : zUpMaxF.GetValue(fdb, owner); } }
	public float zDownMin { get { return Formula.NullFormula(zDownMinF) ? 0 : zDownMinF.GetValue(fdb, owner); } }
	public float zDownMax { get { return Formula.NullFormula(zDownMaxF) ? 0 : zDownMaxF.GetValue(fdb, owner); } }
	public float lineWidthMin { get { return Formula.NullFormula(lineWidthMinF) ? 0 : lineWidthMinF.GetValue(fdb, owner); } }
	public float lineWidthMax { get { return Formula.NullFormula(lineWidthMaxF) ? 0 : lineWidthMaxF.GetValue(fdb, owner); } }

	public float xyDirection { get { return Formula.NullFormula(xyDirectionF) ? 0 : xyDirectionF.GetValue(fdb, owner); } }
	public float zDirection { get { return Formula.NullFormula(zDirectionF) ? 0 : zDirectionF.GetValue(fdb, owner); } }
	public float xyArcMin { get { return Formula.NullFormula(xyArcMinF) ? 0 : xyArcMinF.GetValue(fdb, owner); } }
	public float zArcMin { get { return Formula.NullFormula(zArcMinF) ? 0 : zArcMinF.GetValue(fdb, owner); } }
	public float xyArcMax { get { return Formula.NullFormula(xyArcMaxF) ? 0 : xyArcMaxF.GetValue(fdb, owner); } }
	public float zArcMax { get { return Formula.NullFormula(zArcMaxF) ? 0 : zArcMaxF.GetValue(fdb, owner); } }

	public float rFwdClipMax { get { return Formula.NullFormula(rFwdClipMaxF) ? 0 : rFwdClipMaxF.GetValue(fdb, owner); } }

	public float nWays { get { return Formula.NullFormula(nWaysF) ? 1 : nWaysF.GetValue(fdb, owner); } }

	protected Map map { get { return owner.map; } }

	public delegate PathDecision PathNodeIsValid(Vector3 start, PathNode pn, Character c);

	readonly Vector2[] XYNeighbors = {
		new Vector2(-1, 0),
		new Vector2( 1, 0),
		new Vector2( 0,-1),
		new Vector2( 0, 1)
	};

	public Region() {
	}

	protected bool isEffectRegion=false;
	public bool IsEffectRegion {
		get { return isEffectRegion; }
		set {
			isEffectRegion = value;
			if(regions == null) { return; }
			foreach(Region r in regions) {
				r.IsEffectRegion = value;
			}
		}
	}

	public virtual bool PathNodeMeetsPredicate(
		Vector3 start,
		PathNode pn,
		Character t
	) {
		//short circuit for true/false
		if(predicateF.formulaType == FormulaType.Constant) {
			return predicateF.GetValue(fdb, owner, null, null) != 0;
		}
		Character oldTarget = owner.currentTargetCharacter;
		owner.currentTargetCharacter = t;
		owner.SetArgsFrom(pn.pos, t != null ? (Quaternion?)Quaternion.Euler(0,t.Facing,0) : (Quaternion?)null, "", start);
		float ret = predicateF.GetValue(fdb, owner, null, null);
		owner.currentTargetCharacter = oldTarget;
		return (ret != 0);
	}

	//FIXME: wrong because of reliance on z{Up|Down}{Max|Min}
	public virtual PathDecision PathNodeIsValidRange(Vector3 start, PathNode pn, Character c) {
		float dz = useAbsoluteDZ ? map.SignedDZForMove(pn.position, start) : pn.signedDZ;
		float absDZ = Mathf.Abs(dz);
		//TODO: replace with some type of collision check?
		if(c != null && c != owner.character) {
			if(c.EffectiveTeamID != owner.character.EffectiveTeamID) {
				if(canCrossEnemies || !c.IsTargetable) {
					return (c.IsTargetable && (IsEffectRegion||canHaltAtEnemies||canTargetEnemies||(canMountEnemies && c.IsTargetable && c.IsMountableBy(owner.character) && owner.character.CanMount(c)))) ? PathDecision.Normal : PathDecision.PassOnly;
				} else {
					return (c.IsTargetable && (canHaltAtEnemies||canTargetEnemies||(canMountEnemies && c.IsTargetable && c.IsMountableBy(owner.character) && owner.character.CanMount(c)))) ? PathDecision.Normal : PathDecision.Invalid;
				}
			} else {
				if(!(c.IsTargetable && (IsEffectRegion||canHaltAtEnemies||canTargetEnemies||(canMountEnemies && c.IsTargetable && c.IsMountableBy(owner.character) && owner.character.CanMount(c))))) {
					return PathDecision.PassOnly;
				}
			}
		}
		//TODO: is this actually right? it seems like it should instead be
		//a check to see if there is a tile at pn.pos+(0,0,1)
		if(canCrossWalls) {
			if(IsEffectRegion) {
				if(dz < 0 ? (absDZ > zUpMax) :
						(dz > 0 ? absDZ > zDownMax : false)) {
					return PathDecision.PassOnly;
				}
			} else {
				if((dz == 0 ?
					(zDownMin != 0 && zUpMin != 0) :
						(dz < 0 ? (dz > -zDownMin || dz <= -zDownMax) :
						(dz < zUpMin || dz >= zUpMax)))) {
					return PathDecision.PassOnly;
				}
			}
		}
		return PathDecision.Normal;
	}

	public Dictionary<Vector3, PathNode> LineMoveTilesAround(
		Vector3 here, Quaternion q,
		float xyrmx,
		float zrdmx, float zrumx,
		float xyDir
	) {
		Dictionary<Vector3, PathNode> pickables = new Dictionary<Vector3, PathNode>();
		List<Character> collidedCharacters = new List<Character>();
		float dir, amt, remaining, dd;
		//FIXME: give clients a chance to filter or fix or modify this?
		GetLineMove(
			out dir, out amt, out remaining, out dd,
			collidedCharacters,
			pickables,
			owner.character,
			here, xyDir+q.eulerAngles.y,
			xyrmx,
			zrdmx,
			zrumx
		);
		pickables.Remove(here);
		return pickables;
	}

	public virtual PathNode GetLineMove(
		out float dir,
		out float amt,
		out float remaining,
		out float dd,
		List<Character> collidedCharacters,
		Character chara, Vector3 start
	) {
		return GetLineMove(
			out dir, out amt, out remaining, out dd,
			collidedCharacters,
			null,
			chara,
			start, xyDirection,
			radiusMax, zDownMax, zUpMax
		);
	}

	public virtual PathNode GetNextLineMovePosition(
		Character chara,
		PathNode pn,
		float direction
	) {
		return GetNextLineMovePosition(chara, pn, direction, 1, zDownMax, zUpMax);
	}

	public virtual PathNode GetNextLineMovePosition(
		Character chara,
		PathNode pn,
		float direction,
		float amount,
		float zrdmx,
		float zrumx
	) {
		float dir_ignore=0;
		float amt_ignore=0;
		float rem_ignore=0;
		float dd_ignore=0;
		PathNode ret = GetLineMove(
			out dir_ignore, out amt_ignore, out rem_ignore, out dd_ignore,
			null,
			null,
			chara,
			pn.pos, direction,
			amount,
			zrdmx, zrumx
		);
		if(ret.pos == pn.pos) { return null; }
		return ret;
	}

	public virtual PathNode GetLineMove(
		out float dir,
		out float amt,
		out float remaining,
		out float dd,
		List<Character> collidedCharacters,
		Dictionary<Vector3, PathNode> nodes,

		Character chara,
		Vector3 start, float direction,
		float amount,
		float zrdmx, float zrumx
	) {
		Vector3 here = start;
		PathNode cur = new PathNode(here, null, 0);
		if(nodes != null) {
			nodes[here] = cur;
		}
		Vector2 offset = Vector2.zero;
		LockedFacing f = SRPGUtil.LockFacing(direction, facingLock);
		switch(f) {
			case LockedFacing.XP  : offset = new Vector2( 1, 0); break;
			case LockedFacing.YP  : offset = new Vector2( 0, 1); break;
			case LockedFacing.XN  : offset = new Vector2(-1, 0); break;
			case LockedFacing.YN  : offset = new Vector2( 0,-1); break;
			case LockedFacing.XPYP: offset = new Vector2( 1, 1); break;
			case LockedFacing.XPYN: offset = new Vector2( 1,-1); break;
			case LockedFacing.XNYP: offset = new Vector2(-1, 1); break;
			case LockedFacing.XNYN: offset = new Vector2(-1,-1); break;
			default:
				float fRad = ((float)f)*Mathf.Deg2Rad;
				offset = new Vector2(Mathf.Cos(fRad), Mathf.Sin(fRad));
				break;
		}
		float maxDrop = here.z;
		int soFar = 0;
		float dropDistance = 0;
		Vector3 hereErr = here;
		Debug.Log("start at "+here+" with offset "+offset+" amount "+amount);
		while((soFar < amount) ||
			//stuck prevention: keep going if we would end up stuck
			 ((canCrossWalls &&
		     preventStuckInWalls == StuckPrevention.KeepGoing &&
		     map.TileAt((int)here.x, (int)here.y, (int)here.z+1) != null) ||
		    (!canGlide &&
				 preventStuckInAir == StuckPrevention.KeepGoing &&
		     map.TileAt(here) == null))) {
			Vector3 lastHere = here;
			hereErr.x += offset.x;
			hereErr.y += offset.y;
			here = hereErr;
			here.x = Mathf.Round(hereErr.x);
			here.y = Mathf.Round(hereErr.y);
			MapTile hereT = map.TileAt(here);
			Debug.Log("knock into "+here+"?");
			if(hereT == null) {
				//try to fall?
				Debug.Log("no tile!");
				int lower = map.PrevZLevel((int)here.x, (int)here.y, (int)here.z);
				if(here.x < 0 || here.y < 0 || here.x >= map.size.x || here.y >= map.size.y) {
					Debug.Log("Edge of map!");
					here = lastHere;
					break;
				} else if((!performFall || canGlide) && (soFar+1) < amount) {
					//FIXME: it's the client's responsibility to ensure that gliders
					//don't end up falling into a bottomless pit or onto a character
					cur = new PathNode(here, cur, cur.distance+1-0.01f*maxDrop);
					if(nodes != null) {
						nodes[here] = cur;
					}
					Debug.Log("glide over "+here+"!");
				} else if(lower == -1) {
					//bottomless pit, sort of like a wall?
					Debug.Log("bottomless pit!");
					if(canCrossWalls) {
						here.z = lastHere.z;
						cur = new PathNode(here, cur, cur.distance+1-0.01f*maxDrop);
						if(nodes != null) {
							nodes[here] = cur;
						}
					} else {
						here = lastHere;
						break;
					}
				} else if(performFall) {
					//drop down
					LineMoveFall(
						ref here,
						chara,
						zrdmx,
						maxDrop,
						ref cur,
						nodes,
						collidedCharacters,
						ref dropDistance
					);
				}
			} else {
				//is it a wall? if so, break
				//FIXME: will not work properly with ramps
				int nextZ = map.NextZLevel((int)here.x, (int)here.y, (int)here.z);
				MapTile t = map.TileAt((int)here.x, (int)here.y, nextZ);
				if(t != null &&
				   map.TileAt((int)here.x, (int)here.y, (int)here.z+1) == null) {
					//this tile is above us, sure, but it's way above
					nextZ = (int)here.z;
					t = map.TileAt(here);
				} else {
					here.z = nextZ;
				}
				Character hereChar = map.TargetableCharacterAt(here);
				if(hereChar == chara) { hereChar = null; }
				if(hereChar != null &&
				   collidedCharacters != null &&
				   !collidedCharacters.Contains(hereChar)) {
					collidedCharacters.Add(hereChar);
				}
				Debug.Log("tile z "+nextZ+" vs..."+lastHere.z+"+"+zrumx+"="+(lastHere.z+zrumx));
				if(nextZ > lastHere.z+zrumx) {
					//FIXME: it's the client's responsibility to ensure that wall-crossers
					//don't end up stuck in a wall
					//it's a wall, break
					Debug.Log("wall!");
					if(canCrossWalls) {
						here.z = lastHere.z;
						cur = new PathNode(here, cur, cur.distance+1-0.01f*maxDrop);
						if(nodes != null) {
							nodes[here] = cur;
						}
					} else {
						here = lastHere;
						break;
					}
				} else if(
					!canCrossEnemies &&
					((collidedCharacters != null &&
					collidedCharacters.Count > 0))
				) {
					//break
					//FIXME: it's the client's responsibility to ensure that character-crossers
					//don't end up stuck in a character
					Debug.Log("character "+collidedCharacters[0].name);
					here = lastHere;
					break;
				} else {
					//keep going
					Debug.Log("keep going");
					cur = new PathNode(here, cur, cur.distance+1-0.01f*maxDrop);
					if(nodes != null) {
						nodes[here] = cur;
					}
				}
			}
			Debug.Log("tick forward once");
			soFar++;
		}
		if(canGlide && map.TileAt(here) == null && performFall) {
			//fall
			Debug.Log("it's all over, end glide!");
			LineMoveFall(ref here, chara, zrdmx, maxDrop, ref cur, nodes, collidedCharacters, ref dropDistance);
		}
		//while(in pit/in air || in wall)
		Debug.Log("prev stuck air? "+preventStuckInAir+" null here? "+(map.TileAt(cur.pos) == null));
		Debug.Log("prev stuck walls? "+preventStuckInWalls+" non-null here? "+(map.TileAt((int)cur.pos.x, (int)cur.pos.y, (int)cur.pos.z+1) != null));
		//FIXME: "keep going" strategy in addition to "wind back" strategy
		//if the strategy is Keep Going, then remain in the loop above until we're either in a safe place or at the end of the map; if we're at the end of the map, do the wind back strategy.
		//if the strategy is Wind Back, just do this loop below.
		//if the character strategy is Move Aside, then follow the correct rules... based on where the Wall strategy puts you
		while((preventStuckInAir != StuckPrevention.None && map.TileAt(cur.pos) == null) ||
		      (canCrossWalls && preventStuckInWalls != StuckPrevention.None && (map.TileAt((int)cur.pos.x, (int)cur.pos.y, (int)cur.pos.z+1) != null))) {
			//beginning
			Debug.Log("prev tick");
			if(cur.prev == null) { break; }
			//fall
			if(performFall && preventStuckInAir != StuckPrevention.None && cur.prev.pos.x == cur.pos.x && cur.prev.pos.y == cur.pos.y) {
				if(cur.prev.pos.z != cur.pos.z) {
					float dz = Mathf.Abs(cur.pos.z - cur.prev.pos.z);
					Debug.Log("reverse drop by "+(dz)+" dropDistance "+(dz-zrumx));
					if(dz > zrumx) {
						//if the drop is more than zrumx, reduce dropdistance by the delta
						dropDistance -= (int)(dz - zrumx);
					}
				} else {
					Debug.Log("reverse weird same-pos situation");
				}
				//pop
				cur = cur.prev;
			} else if(canCrossWalls && preventStuckInWalls != StuckPrevention.None) { //step
				//reduce soFar
				Debug.Log("reverse move into wall");
				soFar -= (int)(Mathf.Abs(cur.pos.x - cur.prev.pos.x) + Mathf.Abs(cur.pos.y - cur.prev.pos.y));
				//pop
				cur = cur.prev;
			}
			Debug.Log("tick prev");
		}
		dir = direction;
		amt = amount;
		remaining = Mathf.Max(amount-soFar, 0);
		dd = dropDistance;
		return cur;
	}

	protected virtual bool LineMoveFall(ref Vector3 here, Character chara, float zDownMax, float maxDrop, ref PathNode cur, Dictionary<Vector3, PathNode> nodes, List<Character> collidedCharacters, ref float dropDistance) {
		//FIXME: will not work properly with ramps
		//FIXME: client's responsibility to ensure we don't fall onto a character or into a bottomless pit here
		int lower = map.PrevZLevel((int)here.x, (int)here.y, (int)here.z);
		if(lower != -1) {
			if((cur.pos.z - lower) > zDownMax) {
				dropDistance += here.z - lower;
			}
			Vector3 oldHere = here;
			here.z = lower;
			cur = new PathNode(here, cur, cur.distance+1-0.01f*(maxDrop-(oldHere.z-lower)));
			if(nodes != null) {
				nodes[here] = cur;
			}
			Character c = map.TargetableCharacterAt(here);
			if(c == chara) { c = null; }
			if(c != null && collidedCharacters != null) {
				collidedCharacters.Add(c);
			}
			Debug.Log("fall down to "+here+"!");
			return true;
		}
		Debug.Log("trying to fall, but it's a bottomless pit!");
		return false;
	}

	public virtual PathNode[] GetTilesInRegion(Vector3 pos, Quaternion q) {
	  return GetValidTiles(
			pos, q,
			radiusMin, radiusMax,
			zDownMin, zDownMax,
			zUpMin, zUpMax,
			lineWidthMin, lineWidthMax,
			interveningSpaceType,
			true
		);
	}

	public virtual PathNode[] GetValidTiles(Vector3 tc, Quaternion q) {
		return GetValidTiles(
			tc, q,
			radiusMin, radiusMax,
			zDownMin, zDownMax,
			zUpMin, zUpMax,
			lineWidthMin, lineWidthMax,
			interveningSpaceType
		);
	}

	public virtual PathNode[] GetValidTiles(
		Vector3 tc, Quaternion q,
		float xyrmn, float xyrmx,
		float zrdmn, float zrdmx,
		float zrumn, float zrumx,
		float lwmn, float lwmx,
		InterveningSpaceType spaceType,
		bool returnAllTiles=false
  ) {
		//intervening space selection filters what goes into `nodes` and what
		//nodes get picked next time around—i.e. how prevs get set up.
		// Debug.Log("tc "+tc+" q "+q);
		// Debug.Log("radmaxf "+radiusMaxF+" radmax "+radiusMax);
		// Debug.Log("xyrmn "+xyrmn+" xyrmx "+xyrmx+" st "+spaceType+" rt "+type);
		// Debug.Log("zrdmn "+zrdmn+" zrdmx "+zrdmx+" zrumn "+zrumn+" zrumx "+zrumx);
		//TODO: all should operate with continuous generators as well as grid-based generators
		Vector3 here = SRPGUtil.Trunc(tc);
		Dictionary<Vector3, PathNode> pickables = null;
		switch(type) {
			case RegionType.Cylinder:
				pickables = CylinderTilesAround(here, xyrmx, zrdmx+(useMountingStepBonus?1:0), zrumx+(useMountingStepBonus?1:0), PathNodeIsValidRange);
				break;
			case RegionType.Sphere:
				pickables = SphereTilesAround(here, xyrmx, zrdmx+(useMountingStepBonus?1:0), zrumx+(useMountingStepBonus?1:0), PathNodeIsValidRange);
				break;
			case RegionType.Line:
				pickables = LineTilesAround(here, q, xyrmx, zrdmx+(useMountingStepBonus?1:0), zrumx+(useMountingStepBonus?1:0), xyDirection, zDirection, lwmn, lwmx, PathNodeIsValidRange);
				break;
			case RegionType.LineMove:
				pickables = LineMoveTilesAround(here, q, xyrmx, zrdmx+(useMountingStepBonus?1:0), zrumx+(useMountingStepBonus?1:0), xyDirection);
				break;
			case RegionType.Cone:
				pickables = ConeTilesAround(here, q, xyrmx, zrdmx+(useMountingStepBonus?1:0), zrumx+(useMountingStepBonus?1:0), xyDirection, zDirection, xyArcMin, xyArcMax, zArcMin, zArcMax, rFwdClipMax, PathNodeIsValidRange);
				break;
			case RegionType.Self:
				pickables =	new Dictionary<Vector3, PathNode>(){
					{here, new PathNode(here, null, 0)}
				};
				break;
			case RegionType.Compound:
				pickables =	new Dictionary<Vector3, PathNode>();
				for(int i = 0; i < regions.Length; i++) {
					Region r = regions[i];
					PathNode[] thesePickables = r.GetValidTiles(
						here, q,
						//pass the subregion's formulae for these so
						//that our own, ignored formulae don't clobber them
						r.radiusMin, r.radiusMax,
						r.zDownMin, r.zDownMax,
						r.zUpMin, r.zUpMax,
						r.lineWidthMin, r.lineWidthMax,
						InterveningSpaceType.Pick
					);
					foreach(PathNode p in thesePickables) {
						p.subregion = i;
						pickables[p.pos] = p;
					}
				}
				break;
			case RegionType.NWay:
				pickables =	new Dictionary<Vector3, PathNode>();
				int thisNWays = (int)nWays;
				float offset = q.eulerAngles.y+xyDirection;
				float degreesPerN = 360.0f/thisNWays;
				for(int n = 0; n < thisNWays; n++) {
					float thisAng = SRPGUtil.WrapAngle(offset+degreesPerN*n);
					for(int i = 0; i < regions.Length; i++) {
						Region r = regions[i];
						PathNode[] thesePickables = r.GetValidTiles(
							here, Quaternion.Euler(q.eulerAngles.x, thisAng, q.eulerAngles.z),
							//pass the subregion's formulae for these so
							//that our own, ignored formulae don't clobber them
							r.radiusMin, r.radiusMax,
							r.zDownMin, r.zDownMax,
							r.zUpMin, r.zUpMax,
							r.lineWidthMin, r.lineWidthMax,
							InterveningSpaceType.Pick
						);
						foreach(PathNode p in thesePickables) {
							//FIXME: won't work properly with subregion targeting mode
							p.subregion = i;
							pickables[p.pos] = p;
						}
					}
				}
				break;
			default:
				Debug.LogError("Unknown region type not supported");
				pickables = null;
				break;
		}
		// Debug.Log("pickables "+pickables.Count);
		IEnumerable<PathNode> picked=null;
		switch(spaceType) {
			case InterveningSpaceType.Arc:
				picked = ArcReachableTilesAround(
					here,
					pickables,
					Mathf.Max(xyrmx, lwmx),
					returnAllTiles
				);
				break;
			case InterveningSpaceType.Line:
				picked = LineReachableTilesAround(
					here,
					pickables,
					returnAllTiles
				);
				break;
			case InterveningSpaceType.LineMove:
				picked = pickables.Values;
				break;
			case InterveningSpaceType.Pick:
				picked = PickableTilesAround(
					here,
					pickables
				);
				break;
			case InterveningSpaceType.Path:
			  picked = PathableTilesAround(
					here,
					pickables,
					xyrmx,
					zrdmx,
					zrumx,
					returnAllTiles
				);
				break;
		}
		// Debug.Log("b pickables "+picked.Count());
		picked = picked.Where((n) => {
			Character c = map.TargetableCharacterAt(n.pos);
			if(!PathNodeMeetsPredicate(here, n, c)) { return false; }
			if(c != null) {
				if(c == owner.character) {
					return canTargetSelf;
				} else if(c.EffectiveTeamID == owner.character.EffectiveTeamID) {
					return canTargetFriends ||
						(canMountFriends &&
						 c.IsMountableBy(owner.character) &&
						 owner.character.CanMount(c));
				} else {
					return canTargetEnemies ||
						(canMountEnemies &&
						 c.IsMountableBy(owner.character) &&
						 owner.character.CanMount(c));
				}
			}
			return true;
		}).ToList().AsEnumerable();
		// Debug.Log("c pickables "+picked.Count());
		switch(type) {
			case RegionType.Cylinder:
				return picked.Where(delegate(PathNode n) {
					float xyd = n.XYDistanceFrom(here);
					int signedDZ = useAbsoluteDZ ?
						(int)map.SignedDZForMove(n.position, here) : n.signedDZ;
					return xyd >= xyrmn && xyd <= xyrmx+n.bonusRange &&
				  	(signedDZ <= -zrdmn || signedDZ >= zrumn) &&
						signedDZ >= -zrdmx-(useMountingStepBonus?1:0) &&
						signedDZ <= zrumx+(useMountingStepBonus?1:0);
				}).ToArray();
			case RegionType.Sphere:
				return picked.Where(delegate(PathNode n) {
					float xyzd = n.XYZDistanceFrom(here);
					int signedDZ = useAbsoluteDZ ?
						(int)map.SignedDZForMove(n.position, here) : n.signedDZ;
					return xyzd >= xyrmn && xyzd <= xyrmx+n.bonusRange &&
					  (signedDZ <= -zrdmn || signedDZ >= zrumn) &&
						signedDZ >= -zrdmx-(useMountingStepBonus?1:0) &&
						signedDZ <= zrumx+(useMountingStepBonus?1:0);
				}).ToArray();
			case RegionType.Line:
				return picked.Where(delegate(PathNode n) {
					float xyd = n.radius;
					int xyOff = (int)Mathf.Abs(n.centerOffset.x) + (int)Mathf.Abs(n.centerOffset.y);
					int signedDZ = (int)n.centerOffset.z;
					return xyd >= xyrmn && xyd <= xyrmx+n.bonusRange &&
						xyOff >= lwmn && xyOff <= lwmx &&
					  (signedDZ <= -zrdmn || signedDZ >= zrumn) &&
					  signedDZ >= -zrdmx-(useMountingStepBonus?1:0) &&
						signedDZ <= zrumx+(useMountingStepBonus?1:0);
				}).ToArray();
			case RegionType.LineMove:
				return picked.ToArray();
			case RegionType.Cone:
			//we've already filtered out stuff at bad angles and beyond maxima.
				return picked.Where(delegate(PathNode n) {
					float xyd = n.radius;
					float fwd = n.radius*Mathf.Cos(n.angle);
					int signedDZ = (int)n.centerOffset.z;
					return xyd >= xyrmn &&
						(signedDZ <= -zrdmn || signedDZ >= zrumn) &&
						(rFwdClipMax <= 0 || fwd <= (rFwdClipMax+1));
				}).ToArray();
			default:
				return picked.ToArray();
		}
	}

	public virtual PathNode[] GetValidTiles(PathNode[] allTiles, Quaternion q) {
		Dictionary<Vector3, PathNode> union = new Dictionary<Vector3, PathNode>();
		foreach(PathNode start in allTiles) {
			//take union of all valid tiles
			//TODO: in many cases, this will just be the passed-in tiles. optimize!
			PathNode[] theseValid = GetValidTiles(start.pos, q);
			foreach(PathNode v in theseValid) {
				union[v.pos] = v;
			}
		}
		return union.Values.ToArray();
	}

	//Presumes one character per tile
	public virtual List<Character> CharactersForTargetedTiles(PathNode[] tiles) {
		List<Character> targets = new List<Character>();
		foreach(PathNode pn in tiles) {
			Character c = map.TargetableCharacterAt(pn.pos);
			if(c != null) {
				if((canTargetEnemies || (canMountEnemies && c.IsMountableBy(owner.character) && owner.character.CanMount(c))) &&
				   c.EffectiveTeamID != owner.character.EffectiveTeamID) {
					targets.Add(c);
				}
				if((canTargetFriends || (canMountFriends && c.IsMountableBy(owner.character) && owner.character.CanMount(c))) &&
				   c.EffectiveTeamID == owner.character.EffectiveTeamID) {
					targets.Add(c);
				}
				if(canTargetSelf && c == owner.character) {
					targets.Add(c);
				}
			}
		}
		return targets;
	}
	public PathNode LastPassableTileBeforeTargetedTile(PathNode p) {
		//go back up prev until we hit the last blocking item before src
		PathNode cur = p;
		PathNode lastEnd = p;
		int tries = 0;
		Color red = Color.red;
		// Debug.Log("last passable before "+p);
		while(cur != null) {
			// Debug.Log("cur:"+cur+"prev:"+cur.prev);
			if(cur.prev != null) {
				Debug.DrawLine(map.TransformPointWorld(cur.pos), map.TransformPointWorld(cur.prev.pos), red, 1.0f, false);
			}
			if(cur.isWall && !canCrossWalls) {
				lastEnd = cur.prev;
				// if(cur.prev != null) { Debug.Log("block just before wall "+cur.prev.pos); }
			}
			//FIXME: what about friendlies?
			if(cur.isEnemy && !canCrossEnemies) {
				Character c = map.TargetableCharacterAt(cur.pos);
				if(canHaltAtEnemies || (canMountEnemies && Owner.character.CanMount(c) && c.IsMountableBy(Owner.character))) {
					lastEnd = cur;
					// Debug.Log("block on top of enemy "+cur.pos);
				} else {
					lastEnd = cur.prev;
					// if(cur.prev != null) { Debug.Log("block just before enemy "+cur.prev.pos); }
				}
			}
			cur = cur.prev;
			tries++;
			if(tries > 50) {
				Debug.LogError("Infinite loop in lastPassableTile for "+p);
				return null;
			}
		}
		// Debug.Log("got "+lastEnd);
		return lastEnd;
	}

	public virtual PathNode[] ActualTilesForTargetedTiles(PathNode[] tiles) {
		//for arc and line, this may be different from the requested tile/tiles
		return tiles.
			Select(t => LastPassableTileBeforeTargetedTile(t)).
			Distinct().
			ToArray();
	}

#region Pathing and movement

  void TryAddingJumpPaths(
		PriorityQueue<float, PathNode> queue,
		HashSet<PathNode> closed,
		Dictionary<Vector3, PathNode> pickables,
		List<PathNode> ret,
		PathNode pn,
		int n2x, int n2y,
		float maxRadius,
		float zUpMax,
		float zDownMax,
		float jumpDistance,
		Vector3 start, Vector3 dest,
		bool provideAllTiles
	) {
		//FIXME: do something smart with arcs in the future
  	for(int j = 0; j < jumpDistance; j++) {
  		//don't go further than our move would allow
  		if(pn.distance+2+j > maxRadius) { break; }
  		Vector2 jumpAdj = new Vector2(pn.pos.x+n2x*(j+2), pn.pos.y+n2y*(j+2));
  		bool canJumpNoFurther = false;
  		foreach(int jumpAdjZ in map.ZLevelsWithin((int)jumpAdj.x, (int)jumpAdj.y, (int)pn.pos.z, -1)) {
  			Vector3 jumpPos = new Vector3(jumpAdj.x, jumpAdj.y, jumpAdjZ);
  			float jumpDZ = useAbsoluteDZ ? map.AbsDZForMove(start, pn.pos) : map.AbsDZForMove(jumpPos, pn.pos);
  			if(jumpDZ <= zDownMax) {
  				float addedJumpCost = 2+j-0.01f*(Mathf.Max(zUpMax, zDownMax)-jumpDZ)+1;
  				PathNode jumpPn = new PathNode(jumpPos, pn, pn.distance+addedJumpCost);
  				jumpPn.isLeap = true;
  				jumpPn.isWall = map.TileAt(jumpPos+new Vector3(0,0,1)) != null;
					Character tc = map.TargetableCharacterAt(jumpPos);
  				jumpPn.isEnemy = tc != null && tc.EffectiveTeamID != owner.character.EffectiveTeamID;
					jumpPn.prev = pn;
  				if(pickables.ContainsKey(jumpPos)) {
  					jumpPn.canStop = pickables[jumpPos].canStop;
					} else {
						//can't land here
						continue;
					}
					//FIXME: these ".z == .z || .z==.z+1" checks may be buggy wrt tall tiles
  				if(!provideAllTiles && jumpPn.isWall && !canCrossWalls) {
  					if(jumpPos.z == pn.pos.z || jumpPos.z == pn.pos.z+1) {
  						canJumpNoFurther = true;
  						break;
  					}
  					continue;
  				}
						//FIXME: what about friendlies?
  				if(!provideAllTiles && jumpPn.isEnemy && !canCrossEnemies) {
  					if(jumpPos.z == pn.pos.z || jumpPos.z == pn.pos.z+1) {
  						canJumpNoFurther = true;
  						break;
  					}
						Character c = map.TargetableCharacterAt(jumpPn.pos);
						if(!(canHaltAtEnemies ||
						    (canMountEnemies && Owner.character.CanMount(c) && c.IsMountableBy(Owner.character)))) {
	  					continue;
						}
  				}
					// Debug.Log("enqueue leap to "+jumpPn.pos);
					queue.Enqueue(jumpPn.distance+Mathf.Abs(jumpPos.x-dest.x)+Mathf.Abs(jumpPos.y-dest.y)+Mathf.Abs(jumpPos.z-dest.z), jumpPn);
  			} else if(jumpAdjZ > pn.pos.z) { //don't jump upwards or through a wall
  				MapTile jt = map.TileAt(jumpPos);
  				if(!provideAllTiles && jt != null && jt.z <= pn.pos.z+2 && !canCrossWalls) { canJumpNoFurther = true; }
  				break;
  			}
  		}
  		if(canJumpNoFurther) {
  			break;
  		}
  	}
  }
//rewrite as several smaller functions, one for each space type
//√ pick -- anywhere within region, prev nodes are all null
//√ path -- anywhere -reachable- within region, prev nodes lead back to start by walking path
//√ line -- anywhere within direct line from start, prev nodes lead back to start
//√ arc  -- anywhere within arc, prev nodes lead in a parabola
	bool AddPathTo(
		PathNode destPn, Vector3 start,
		Dictionary<Vector3, PathNode> pickables,
		List<PathNode> ret,
		float maxRadius, //max cost for path
		float zDownMax,  //apply to each step
		float zUpMax,    //apply to each step
		bool provideAllTiles
	) {
		if(ret.Contains(destPn)) {
			// Debug.Log("dest "+destPn.pos+" is already in ret");
			return true;
		}
		Vector3 dest = destPn.pos;
		if(dest == start) {
			// Debug.Log("ret gets "+dest+" which == "+start);
			ret.Add(destPn);
			return true;
		}
		// Debug.Log("seek path to "+dest);
		int jumpDistance = (int)(zDownMax/2);
		int headroom = 1;
		HashSet<PathNode> closed = new HashSet<PathNode>();
		var queue = new PriorityQueue<float, PathNode>();
		if(!pickables.ContainsKey(start)) { return false; }
		PathNode startNode = pickables[start];
		queue.Enqueue(startNode.distance, startNode);
		int tries = 0;
		const int tryLimit = 200;
		while(!queue.IsEmpty && tries < tryLimit) {
			tries++;
			PathNode pn = queue.Dequeue();
				// Debug.Log("dequeue "+pn);
			//skip stuff we've seen already
			if(closed.Contains(pn)) {
//				Debug.Log("closed");
				continue;
			}
			//if we have a path, or can reach a node that is in ret, add the involved nodes to ret if they're not yet present
			if(pn.pos == dest) {
				//add all prevs to ret
				PathNode cur = pn;
					// Debug.Log("found path from "+start+" to "+dest+":"+pn);
				while(cur.prev != null) {
					// Debug.Log(""+cur.pos);
					if(!ret.Contains(cur)) {
						ret.Add(cur);
					}
					cur = cur.prev;
				}
				//and return true
				return true;
			}
			closed.Add(pn);
			//each time around, enqueue XYZ neighbors of cur that are in pickables and within zdownmax/zupmax. this won't enable pathing through walls, but that's kind of an esoteric usage anyway. file a bug. remember the jumping to cross gaps if a neighbor doesn't have a tile there (extend the neighbor search until we're past zDownMax/2)
			if(pn.xyDistanceFromStart == maxRadius) {
				//don't bother trying to add any more points, they'll be too far
				continue;
			}
			if(pn.isEnemy && !canCrossEnemies && !provideAllTiles) { continue; }
			bool zStepBonusFrom = false;
			if(useMountingStepBonus && (canMountFriends || canMountEnemies)) {
				var steppables = map.CharactersAt(pn.pos).
					Where(ch =>
						(Owner is MoveSkillDef && !((Owner as MoveSkillDef).remainMounted) && ch == Owner.character.mountedCharacter) ||
						(ch.IsTargetable &&
						(ch.IsMountableBy(Owner.character) &&
						 Owner.character.CanMount(ch)))
					).ToArray();
				if(steppables.Length > 0 &&
				   ((canMountFriends &&
				     steppables.Any(stp => stp.IsAlly(Owner.character))) ||
				    (canMountEnemies &&
				     steppables.Any(stp => stp.IsEnemy(Owner.character))))) {
					zStepBonusFrom = true;
				}
			}
      foreach(Vector2 n2 in XYNeighbors)
      {
				if(pn.xyDistanceFromStart+n2.x+n2.y > maxRadius && !provideAllTiles) {
					continue;
				}
				float px = pn.pos.x+n2.x;
				float py = pn.pos.y+n2.y;
				// Debug.Log("search at "+px+", "+py + " (d "+n2.x+","+n2.y+")"+" (xyd "+(pn.xyDistanceFromStart+n2.x+n2.y)+")");
				foreach(int adjZ in map.ZLevelsWithin((int)px, (int)py, (int)pn.pos.z, -1)) {
					Vector3 pos = SRPGUtil.Trunc(new Vector3(px, py, adjZ));
					bool zStepBonusTo = false;
					if(useMountingStepBonus && (canMountFriends || canMountEnemies)) {
						var steppables = map.CharactersAt(pos).
							Where(ch =>
								(Owner is MoveSkillDef && !((Owner as MoveSkillDef).remainMounted) && ch == Owner.character.mountedCharacter) ||
								(ch.IsTargetable &&
								(ch.IsMountableBy(Owner.character) &&
								 Owner.character.CanMount(ch)))
							).ToArray();
						if(steppables.Length > 0 &&
						   ((canMountFriends &&
						     steppables.Any(stp => stp.IsAlly(Owner.character))) ||
						    (canMountEnemies &&
						     steppables.Any(stp => stp.IsEnemy(Owner.character))))) {
							zStepBonusTo = true;
						}
					}

					float dz = useAbsoluteDZ ?
						map.SignedDZForMove(pos, start) :
						map.SignedDZForMove(pos, pn.pos);

					if(dz > 0 && dz > (zStepBonusFrom ? zUpMax+1 : zUpMax)) {
						continue;
					}
					if(dz < 0 && Mathf.Abs(dz) > (zStepBonusTo ? zDownMax+1 : zDownMax)) {
						continue;
					}
					if(!provideAllTiles && dz > 0 && !canCrossWalls && map.ZLevelsWithinLimits((int)pn.pos.x, (int)pn.pos.y, (int)pn.pos.z, adjZ+headroom).Length != 0) {
						continue;
					}
					if(!provideAllTiles && dz < 0 && !canCrossWalls && map.ZLevelsWithinLimits((int)pos.x, (int)pos.y, adjZ, (int)pn.pos.z+headroom).Length != 0) {
						continue;
					}
					if(map.TileAt(pos) == null) {
						//can't path through empty space
						continue;
					}
					PathNode next=null;
        	if(!pickables.TryGetValue(pos, out next)) {
						continue;
					}
					if(closed.Contains(next)) {
						//skip stuff we've already examined
						continue;
					}
					if(next.distance > 0 && next.distance <= pn.distance) {
						//skip anything that's got a better path to it than we can offer
							// Debug.Log("Don't bother looking via "+next);
						continue;
					}
					if(adjZ < pn.pos.z) {
						//try to jump across me
						TryAddingJumpPaths(queue, closed, pickables, ret, pn, (int)n2.x, (int)n2.y, maxRadius, zUpMax, zDownMax, jumpDistance, start, dest, provideAllTiles);
					}
					float addedCost = Mathf.Abs(n2.x)+Mathf.Abs(n2.y)-0.01f*(Mathf.Max(zUpMax+1, zDownMax+1)-Mathf.Abs(pn.pos.z-adjZ)); //-0.3f because we are not a leap
					next.isWall = map.TileAt(pos+new Vector3(0,0,1)) != null;
					Character c = map.TargetableCharacterAt(pos);
					next.isEnemy = c != null && c.EffectiveTeamID != owner.character.EffectiveTeamID;
					next.distance = pn.distance+addedCost;
					next.prev = pn;
					if(!provideAllTiles && next.isWall && !canCrossWalls) {
						continue;
					}
					//FIXME: what about friendlies?
					if(!provideAllTiles && next.isEnemy && !canCrossEnemies && !canHaltAtEnemies && !(canMountEnemies && c.IsMountableBy(owner.character) && owner.character.CanMount(c))) {
						continue;
					}
					queue.Enqueue(next.distance+Mathf.Abs(pos.x-dest.x)+Mathf.Abs(pos.y-dest.y)+Mathf.Abs(pos.z-dest.z), next);
					// Debug.Log("enqueue "+next.pos+" with cost "+next.distance);
				}
      }
		}
		// Debug.Log("tries: "+tries);
		if(tries >= tryLimit) {
			Debug.LogError("escape infinite loop in pathing from "+start+" to "+destPn.pos);
		}
		return false;
	}

	public IEnumerable<PathNode> PathableTilesAround(
		Vector3 here,
		Dictionary<Vector3, PathNode> pickables,
		float xyrmx,
		float zrdmx,
		float zrumx,
		bool provideAllTiles
	) {
		var ret = new List<PathNode>();
		Vector3 truncStart = SRPGUtil.Trunc(here);
		var sortedPickables = pickables.Values.
			OrderBy(p => p.XYDistanceFrom(here)).
			ThenBy(p => Mathf.Abs(p.SignedDZFrom(here)));
		//TODO: cache and reuse partial search results
		foreach(PathNode pn in sortedPickables) {
			//find the path
//			if(pn.prev != null) { Debug.Log("pos "+pn.pos+" has prev "+pn.prev.pos); continue; }
			AddPathTo(pn, truncStart, pickables, ret, xyrmx, zrdmx, zrumx, provideAllTiles);
		}
		return ret;
	}
	public Dictionary<Vector3, PathNode> CylinderTilesAround(
		Vector3 start,
		float maxRadiusF,
		float zDownMax,
		float zUpMax,
		PathNodeIsValid isValid
	) {
		var ret = new Dictionary<Vector3, PathNode>();
		//for all tiles at all z levels with xy manhattan distance < max radius and z manhattan distance between -zDownMax and +zUpMax, make a node if that tile passes the isValid check
		float maxBonus = useArcRangeBonus ? Mathf.Max(zDownMax, zUpMax)/2.0f : 0;
		float maxRadius = Mathf.Floor(maxRadiusF);
		float minR = -maxRadius-maxBonus;
		float maxR = maxRadius+maxBonus;
		for(float i = minR; i <= maxR; i++) {
			for(float j = minR; j <= maxR; j++) {
				if(Mathf.Abs(i)+Mathf.Abs(j) > maxRadius+Mathf.Abs(maxBonus)) {
					continue;
				}
				Vector2 here = new Vector2(start.x+i, start.y+j);
				// Debug.Log("gen "+i+","+j+" here:"+here);
				if(here.x < 0 || here.y < 0 ||
				   here.x >= map.size.x || here.y >= map.size.y) {
					continue;
				}

				IEnumerable<int> levs = map.ZLevelsWithin((int)here.x, (int)here.y, (int)start.z, -1);
				foreach(int adjZ in levs) {
					Vector3 pos = new Vector3(here.x, here.y, adjZ);
					//CHECK: is this right? should it just be the signed delta? or is there some kind of "signed delta between lowest/highest points for z- and highest/lowest points for z+" nonsense?
					float signedDZ = map.SignedDZForMove(pos, start);
					// Debug.Log("signed dz:"+signedDZ+" at "+pos.z+" from "+start.z);
					if(useAbsoluteDZ && (signedDZ < -zDownMax || signedDZ > zUpMax)) {
						continue;
					}
					float bonus = useArcRangeBonus ? -signedDZ/2.0f : 0;
//					Debug.Log("bonus at z="+adjZ+"="+bonus);
					if(Mathf.Abs(i) + Mathf.Abs(j) > maxRadius+bonus) {
						continue;
					}
//					float adz = Mathf.Abs(signedDZ);
					PathNode newPn = new PathNode(pos, null, 0/*i+j+0.01f*adz*/);
					newPn.bonusRange = bonus;
					Character c = map.TargetableCharacterAt(pos);
					if(c != null &&
						 c.EffectiveTeamID != owner.character.EffectiveTeamID) {
						newPn.isEnemy = true;
					  // Debug.Log("enemy pn "+newPn);
					}
					MapTile aboveT = map.TileAt((int)pos.x, (int)pos.y, (int)pos.z+1);
					if(aboveT != null) {
						newPn.isWall = true;
					}
					PathDecision decision = isValid(start, newPn, map.CharacterAt(pos));
					if(decision == PathDecision.PassOnly) {
						newPn.canStop = false;
					}
					// Debug.Log("decision "+decision);
					if(decision != PathDecision.Invalid) {
						ret.Add(pos, newPn);
					}
				}
			}
		}
		return ret;
	}

	public Dictionary<Vector3, PathNode> SphereTilesAround(
		Vector3 start,
		float maxRadiusF,
		float zDownMax,
		float zUpMax,
		PathNodeIsValid isValid
	) {
		var ret = new Dictionary<Vector3, PathNode>();
		//for all tiles at all z levels with xyz manhattan distance < max radius and z manhattan distance between -zDownMax and +zUpMax, make a node if that tile passes the isValid check
		float maxBonus = useArcRangeBonus ? Mathf.Max(zDownMax, zUpMax)/2.0f : 0;
		float maxRadius = Mathf.Floor(maxRadiusF);
		float minR = -maxRadius-maxBonus;
		float maxR = maxRadius+maxBonus;
		for(float i = minR; i <= maxR; i++) {
			for(float j = minR; j <= maxR; j++) {
				if(Mathf.Abs(i)+Mathf.Abs(j) > maxRadius+Mathf.Abs(maxBonus)) {
					continue;
				}
//				Debug.Log("gen "+i+","+j);

				Vector2 here = new Vector2(start.x+i, start.y+j);
				IEnumerable<int> levs = map.ZLevelsWithin((int)here.x, (int)here.y, (int)start.z, -1);
				foreach(int adjZ in levs) {
					Vector3 pos = new Vector3(here.x, here.y, adjZ);
					//CHECK: is this right? should it just be the signed delta? or is there some kind of "signed delta between lowest/highest points for z- and highest/lowest points for z+" nonsense?
					float signedDZ = map.SignedDZForMove(pos, start);
					float adz = Mathf.Abs(signedDZ);
//					Debug.Log("signed dz:"+signedDZ+" at "+pos.z+" from "+start.z);
					if(useAbsoluteDZ && (signedDZ < -zDownMax || signedDZ > zUpMax)) {
						continue;
					}
					float bonus = useArcRangeBonus ? -signedDZ/2.0f : 0;
//					Debug.Log("bonus at z="+adjZ+"="+bonus);
					float radius = Mathf.Abs(i) + Mathf.Abs(j) + adz;
					if(radius > maxRadius+bonus) {
						continue;
					}
					PathNode newPn = new PathNode(pos, null, 0/*i+j+0.01f*adz*/);
					newPn.radius = radius;
					newPn.bonusRange = bonus;
					Character c = map.TargetableCharacterAt(pos);
					if(c != null &&
						 c.EffectiveTeamID != owner.character.EffectiveTeamID) {
						newPn.isEnemy = true;
					}
					MapTile aboveT = map.TileAt((int)pos.x, (int)pos.y, (int)pos.z+1);
					if(aboveT != null) {
						newPn.isWall = true;
					}
					PathDecision decision = isValid(start, newPn, map.CharacterAt(pos));
					if(decision == PathDecision.PassOnly) {
						newPn.canStop = false;
					}
					if(decision != PathDecision.Invalid) {
						ret.Add(pos, newPn);
					}
				}
			}
		}
		return ret;
	}

	public Dictionary<Vector3, PathNode> LineTilesAround(
		Vector3 here, Quaternion q,
		float xyrmx,
		float zrdmx, float zrumx,
		float xyDirection,
		float zDirection,
		float lwmn, float lwmx,
		PathNodeIsValid isValid
	) {
		var ret = new Dictionary<Vector3, PathNode>();
		float xyTheta = (xyDirection+q.eulerAngles.y)*Mathf.Deg2Rad;
		float zPhi = (zDirection+q.eulerAngles.z)*Mathf.Deg2Rad;
		for(int r = 0; r <= (int)xyrmx; r++) {
			//FIXME: if xyTheta != 0, oxy and oz calculations may be wrong
			//FIXME: use of zPhi (cos, cos, sin) is not correct -- consider pointing vertically. it shouldn't collapse to a single column!
			Vector3 linePos = new Vector3(Mathf.Cos(xyTheta)*Mathf.Cos(zPhi)*r, Mathf.Sin(xyTheta)*Mathf.Cos(zPhi)*r, Mathf.Sin(zPhi)*r);
			for(int lwo = -(int)lwmx; lwo <= (int)lwmx; lwo++) {
				//FIXME: use of zPhi (cos, cos, 0) is not correct -- consider pointing vertically. it shouldn't collapse to a single column!
				Vector3 oxy = new Vector3(Mathf.Sin(xyTheta)*Mathf.Cos(zPhi)*lwo, Mathf.Cos(xyTheta)*Mathf.Cos(zPhi)*lwo, 0);
				for(int zRad = -(int)zrdmx; zRad <= (int)zrumx; zRad++) {
					//FIXME: (sin,sin,cos) here may be way wrong-- it really depends on xyTheta as well!!
					Vector3 oz = new Vector3(Mathf.Sin(zPhi)*zRad, Mathf.Sin(zPhi)*zRad, Mathf.Cos(zPhi)*zRad);
					Vector3 pos = SRPGUtil.Round(new Vector3(
						here.x+linePos.x+oxy.x+oz.x,
						here.y+linePos.y+oxy.y+oz.y,
						here.z+linePos.z+oxy.z+oz.z
					));
					if(map.TileAt(pos) == null) { continue; }
					PathNode pn = new PathNode(pos, null, 0);
					pn.radius = r;
					pn.angle = xyDirection;
					pn.altitude = zDirection;
					pn.centerOffset = new Vector3(lwo, 0, zRad);
					//FIXME: duplicated across other generators
					Character c = map.TargetableCharacterAt(pos);
					if(c != null &&
						 c.EffectiveTeamID != owner.character.EffectiveTeamID) {
						pn.isEnemy = true;
					}
					//FIXME: ramps. also, this might not make any sense wrt immediately stacked tiles.
					MapTile aboveT = map.TileAt((int)pos.x, (int)pos.y, (int)pos.z+1);
					if(aboveT != null) {
						pn.isWall = true;
					}
					PathDecision decision = isValid(here, pn, map.CharacterAt(pos));
					if(decision == PathDecision.PassOnly) {
						pn.canStop = false;
					}
					if(decision != PathDecision.Invalid) {
						ret[pos] = pn;
					}
				}
			}
		}
		return ret;
	}

	public Dictionary<Vector3, PathNode> ConeTilesAround(
		Vector3 here, Quaternion q,
		float xyrmx,
		float zrdmx, float zrumx,
		float xyDirection,
		float zDirection,
		float xyArcMin, float xyArcMax,
		float zArcMin, float zArcMax,
		float rFwdClipMax,
		PathNodeIsValid isValid
	) {
		float centerXYAng = (xyDirection+q.eulerAngles.y);
		float cosCenterXYAng = Mathf.Cos(Mathf.Deg2Rad*centerXYAng);
		float sinCenterXYAng = Mathf.Sin(Mathf.Deg2Rad*centerXYAng);
		float centerZAng = Mathf.Deg2Rad*(zDirection+q.eulerAngles.z);
		float cosCenterZAng = Mathf.Cos(Mathf.Deg2Rad*centerZAng);
		float sinCenterZAng = Mathf.Sin(Mathf.Deg2Rad*centerZAng);
		//just clip a sphere for now
		return SphereTilesAround(here, xyrmx, float.MaxValue, float.MaxValue, isValid).Where(delegate(KeyValuePair<Vector3, PathNode> pair) {
			PathNode n = pair.Value;
			float xyd = Vector3.Distance(n.pos, here);
			float xyArcTolerance = xyd == 0 ? 0 : (0.5f/xyd)*Mathf.Rad2Deg;
			float zArcTolerance = xyd == 0 ? 0 : (0.5f/xyd)*Mathf.Rad2Deg;

			float xyAng = Mathf.Rad2Deg*Mathf.Atan2(n.pos.y-here.y, n.pos.x-here.x)-q.eulerAngles.y;
			float zAng = Mathf.Rad2Deg*Mathf.Atan2(n.pos.z-here.z, xyd)-q.eulerAngles.z;
			//FIXME: (cos,cos,sin) is wrong, consider vertical pointing
			Vector3 centerPoint = new Vector3(cosCenterXYAng*sinCenterZAng*xyd, sinCenterXYAng*sinCenterZAng*xyd, cosCenterZAng*xyd);
			//FIXME: these four lines certainly do not belong here! side effects in a filter, bleh!
			n.radius = xyd;
			n.angle = xyAng;
			n.altitude = zAng;
			n.centerOffset = n.pos - centerPoint;
			float fwd = n.radius*Mathf.Cos(n.angle);
			float signedDZ = n.centerOffset.z;
			return xyd <= xyrmx+n.bonusRange &&
			  (SRPGUtil.AngleBetween(xyAng, xyDirection+xyArcMin-xyArcTolerance, xyDirection+xyArcMax+xyArcTolerance) ||
			   SRPGUtil.AngleBetween(xyAng, xyDirection-xyArcMax-xyArcTolerance, xyDirection-xyArcMin+xyArcTolerance)) &&
			  (SRPGUtil.AngleBetween(zAng, zDirection+zArcMin-zArcTolerance, zDirection+zArcMax+zArcTolerance) ||
			   SRPGUtil.AngleBetween(zAng, zDirection-zArcMax-zArcTolerance, zDirection-zArcMin+zArcTolerance)) &&
 				(rFwdClipMax <= 0 || fwd <= (rFwdClipMax+1)) &&
			  signedDZ >= -zrdmx && signedDZ <= zrumx;
		}).ToDictionary(pair => pair.Key, pair => pair.Value);
	}

	public IEnumerable<PathNode> PickableTilesAround(
		Vector3 here,
		Dictionary<Vector3, PathNode> pickables
	) {
		return pickables.Values;
	}

	bool AnglesWithin(float a, float b, float eps) {
		return Mathf.Abs(Mathf.DeltaAngle(a, b)) < eps;
	}

	public IEnumerable<PathNode> LineReachableTilesAround(
		Vector3 start,
		Dictionary<Vector3, PathNode> pickables,
		bool provideAllTiles
	) {
		var ret = new List<PathNode>();
		//we bump start up by 1 in z so that the line can come from the head rather than the feet
		Vector3 truncStart = SRPGUtil.Trunc(start+new Vector3(0,0,1));
		var sortedPickables = pickables.Values.
			OrderBy(p => p.XYDistanceFrom(start)).
			ThenBy(p => Mathf.Abs(p.SignedDZFrom(start)));
		//improve efficiency by storing intermediate calculations -- i.e. the tiles on the line from end to start
		foreach(PathNode pn in sortedPickables) {
			if(pn.prev != null) { continue; }
			Vector3 here = truncStart;
			Vector3 truncEnd = SRPGUtil.Trunc(pn.pos);
			Vector3 truncHere = truncStart;
			if(truncStart == truncEnd) {
				ret.Add(pn);
				continue;
			}
			Vector3 d = truncEnd-truncHere;
			//HACK: moves too fast and produces infinite loops
			//when normalized d is too big relative to the actual distance
			d = d.normalized;
			PathNode cur=null;
			pickables.TryGetValue(truncHere, out cur);
			if(cur == null) {
				// Debug.Log("new pn at "+truncHere);
				cur = new PathNode(truncHere, null, 0);
				if(map.TileAt(truncHere+new Vector3(0,0,1)) != null) {
					cur.isWall = true;
				}
				Character mc = map.TargetableCharacterAt(truncHere);
				cur.isEnemy = mc != null && mc.EffectiveTeamID != Owner.character.EffectiveTeamID;
			} else {
				// Debug.Log("reusing "+cur);
			}
			Vector3 prevTrunc = here;
			int tries = 0;
			while(truncHere != truncEnd) {
				here += d;
				truncHere = SRPGUtil.Round(here);
				if(prevTrunc == truncHere) { continue; }
				prevTrunc = truncHere;
				PathNode herePn = null;
				if(pickables.ContainsKey(truncHere)) {
					herePn = pickables[truncHere];
				} else { //must be empty air
					// Debug.Log("empty air at "+truncHere);
					herePn = new PathNode(truncHere, null, 0);
					if(map.TileAt(truncHere+new Vector3(0,0,1)) != null) {
						herePn.isWall = true;
					}
					Character mc = map.TargetableCharacterAt(truncHere);
					herePn.isEnemy = mc != null && mc.EffectiveTeamID != Owner.character.EffectiveTeamID;
					pickables.Add(truncHere, herePn);
				}
				herePn.prev = cur;
				// Debug.Log("here: "+herePn+" tc "+map.TargetableCharacterAt(herePn.pos));
				cur = herePn;
				if(herePn.isWall && !canCrossWalls && !provideAllTiles) {
					//don't add this node or parents and break now
					// Debug.Log("break at wall");
					break;
				}
				//FIXME: what about friendlies?
				Character c = map.TargetableCharacterAt(herePn.pos);
				if(herePn.isEnemy && !canCrossEnemies && !canHaltAtEnemies && !provideAllTiles && !(canMountEnemies && c.IsMountableBy(owner.character) && owner.character.CanMount(c))) {
					//don't add this node and break now
					// Debug.Log("break at enemy a");
					break;
				}
				if(truncHere == truncEnd || tries > 50) {
					ret.Add(pn);
					// Debug.Log("break at end");
					break;
				}
				if(cur.isEnemy && !canCrossEnemies && !provideAllTiles) {
					// Debug.Log("break at enemy b");
					break;
				}
				tries++;
			}
			if(tries >= 50) {
				Debug.LogError("infinite loop while walking by "+d+" to "+truncStart+" from "+truncHere);
			}
		}
		return ret;
	}

	bool FindArcFromTo(
		Vector3 startPos, Vector3 pos, Vector3 dir,
		float d, float theta, float v, float g, float dt,
		Dictionary<Vector3, PathNode> pickables,
		bool provideAllTiles
	) {
		// Color color = new Color(Random.value, Random.value, Random.value, 0.6f);

		Vector3 prevPos = startPos;
		float sTH = Mathf.Sin(theta);
		float cosTH = Mathf.Cos(theta);
		float endT = d / (v * cosTH);
		if(endT < 0) { Debug.LogError("Bad end T! "+endT); }
		float t = 0;
		while(t < endT && prevPos != pos) {
			//x(t) = v*cos(45)*t
			float xDist = v * cosTH * t;
			float xr = xDist / d;
			//y(t) = v*sin(45)*t - (g*t^2)/2
			float y = v * sTH * t - (g * t * t)/2.0f;
			Vector3 testPos = SRPGUtil.Round(new Vector3(startPos.x, startPos.y, startPos.z+y) + xr*dir);
			if(testPos == prevPos && t != 0) { t += dt; continue; }
			// if(pos.x==1&&pos.y==0&&pos.z==0) {  }
			// Debug.DrawLine(map.TransformPointWorld(prevPos), map.TransformPointWorld(testPos), color, 1.0f);
			PathNode pn = null;
			if(pickables.ContainsKey(testPos)) {
				pn = pickables[testPos];
				if(testPos != prevPos) {
					pn.prev = pickables[prevPos];
				}
				pn.distance = xDist;
			} else {
				pickables[testPos] = pn = new PathNode(testPos, (testPos != prevPos && pickables.ContainsKey(prevPos) ? pickables[prevPos] : null), xDist);
				pn.canStop = false;
				pn.isWall = /*map.TileAt(testPos) != null && */map.TileAt(testPos+new Vector3(0,0,1)) != null;
				Character tc = map.TargetableCharacterAt(testPos);
				pn.isEnemy = tc != null && tc.EffectiveTeamID != owner.character.EffectiveTeamID;
			}
			if(prevPos.z < testPos.z && map.TileAt(testPos) != null) {
				pn.isWall = true;
			}
			if(pn.isWall && !canCrossWalls && !provideAllTiles) {
				//no good!
				// Debug.Log("wall, can't cross at testpos "+testPos);
				break;
			}
			//FIXME: what about friendlies?
			Character c = map.TargetableCharacterAt(pn.pos);
			if(pn.isEnemy && !canCrossEnemies && !canHaltAtEnemies && !provideAllTiles && !(canMountEnemies && c.IsMountableBy(owner.character) && owner.character.CanMount(c))) {
				//no good!
				// Debug.Log("enemy, can't cross, can't halt at testpos "+testPos);
				break;
			}
			if(pn.prev != null && pn.prev.isEnemy && !canCrossEnemies && !provideAllTiles && !(canMountEnemies && c.IsMountableBy(owner.character) && owner.character.CanMount(c))) {
				//no good!
				// Debug.Log("halted already at testpos "+testPos);
				break;
			}
 // Debug.Log("go through testpos "+testPos);
			prevPos = testPos;
			t += dt;
		}
		// Debug.DrawLine(map.TransformPointWorld(prevPos), map.TransformPointWorld(pos), color, 1.0f);
		if(prevPos == pos) {
			return true;
		} else if(t >= endT) {
			Debug.Log("T passed "+endT+" without reaching "+pos+", got "+prevPos);
		}
		return false;
	}

	public IEnumerable<PathNode> ArcReachableTilesAround(
		Vector3 here,
		Dictionary<Vector3, PathNode> pickables,
		float maxRadius,
		bool provideAllTiles
	) {
		List<PathNode> ret = new List<PathNode>();
		Vector3 startPos = here+new Vector3(0,0,1);
		if(!pickables.ContainsKey(startPos) && map.TileAt(startPos) == null) {
			pickables[startPos] = new PathNode(startPos, null, 0);
		}
		float g = 9.8f;
		float dt = 0.05f;
		float v = Mathf.Sqrt(g*maxRadius);
		foreach(var pair in pickables.ToList()) {
			Vector3 pos = pair.Key;
			// Debug.Log("check "+pos);
			Vector3 dir = new Vector3(pos.x-startPos.x, pos.y-startPos.y, 0);
			PathNode posPn = pair.Value;
			float d = Mathf.Sqrt(dir.x*dir.x+dir.y*dir.y);
			if(d == 0) { //trivial success, same tile
				posPn.velocity = 0;
				posPn.altitude = 180;
				ret.Add(posPn);
				continue;
			}
			//theta = atan((v^2±sqrt(v^4-g(gx^2+2yv^2))/gx)) for x=distance, y=target y
			//either root, if it's not imaginary, will work! otherwise, bail because v is too small
			float thX = d;
			float thY = pos.z-startPos.z;
			float thSqrtTerm = Mathf.Pow(v,4)-g*(g*thX*thX+2*thY*v*v);
			if(thSqrtTerm < 0) { //impossible to hit with current v
				// Debug.Log("nope, thSq is 0 for thX "+thX+" thY "+thY+" v "+v);
				continue;
			}
			float theta1 = Mathf.Atan((v*v+Mathf.Sqrt(thSqrtTerm))/(g*thX));
			float theta2 = Mathf.Atan((v*v-Mathf.Sqrt(thSqrtTerm))/(g*thX));
			//try 1, then 2. we also accept aiming downward.
			if(FindArcFromTo(startPos, pos, dir, d, theta1, v, g, dt, pickables, provideAllTiles)) {
				posPn.velocity = v;
				posPn.altitude = theta1;
				ret.Add(posPn);
			} else if(FindArcFromTo(startPos, pos, dir, d, theta2, v, g, dt, pickables, provideAllTiles)) {
				posPn.velocity = v;
				posPn.altitude = theta2;
				ret.Add(posPn);
			} else {
				// Debug.Log("nope");
			}
		}
		return ret;
	}

#endregion
}