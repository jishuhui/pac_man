using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PathNode {
	public int[] point{ get; set; }
	public PathNode cameFrom{ get; set; }
	public int g{ get; set; }
	public int h{ get; set; }
	public int f {
		get {
			return this.g + this.h;
		}
	}
}

public class CharController : MonoBehaviour {
	public int[] currentPos;
	public char[,] level;
	public Direction currentDirection = Direction.none;
	Direction targetDirection = Direction.none;
	Vector3 velocity;
	List<Direction> availableDirections;
	List<int[]> path;
	public bool debug;

	// Use this for initialization
	void Start () {
		availableDirections = GetAvailableDirections (currentPos [0], currentPos [1]);
		path = new List<int[]> ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		switch (currentDirection) {
		case Direction.down: velocity = new Vector3(0, -1, 0); break;
		case Direction.up: velocity = new Vector3(0, 1, 0); break;
		case Direction.left: velocity = new Vector3(-1, 0, 0); break;
		case Direction.right: velocity = new Vector3(1, 0, 0); break;
		default: velocity = Vector3.zero; break;
		}
		Move (velocity);
	}

	//Sets the destination point and starts moving
	public void MoveToPoint(Vector2 p) {
		int[] dest = new int[2]{Mathf.FloorToInt (p.x), Mathf.FloorToInt (p.y)};
		path = CalculatePath (dest);
		if (debug) {
			GameObject[] cubes = GameObject.FindGameObjectsWithTag ("debug_cube");
			foreach(GameObject go in cubes) {
				Destroy(go);
			}
			foreach (int[] po in path) {
				GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
				Destroy(go.collider);
				go.transform.position = new Vector3(po[0]+0.5f, level.GetLength(1) - 1 - po[1] + 0.5f, -0.2f);
				go.transform.localScale = Vector3.one*0.28f;
				go.tag = "debug_cube";
			}
		}
		SetStartAIDirection ();
	}

	void SetStartAIDirection() {
		//path.RemoveAt (0);
		if (path.Count>1) {
			int[] vec = new int[2]{0,0};
			if (ComparePoints(path[1], currentPos)) {
				//if (path.Count>2) {
				vec = new int[2]{path [0] [0] - currentPos [0], path [0] [1] - currentPos [1]};
				//}
			} else {
				vec = new int[2]{path [1] [0] - currentPos [0], path [1] [1] - currentPos [1]};
			}
			//int[] vec = new int[2]{path [2] [0] - path [1] [0], path [2] [1] - path [1] [1]};
			if (vec[0]>0) {
				//targetDirection = Direction.right;
				TrySetMoveDirection(Direction.right);
				//currentDirection = Direction.right;
			}
			if (vec[0]<0) {
				//targetDirection = Direction.left;
				TrySetMoveDirection(Direction.left);
				//currentDirection = Direction.left;
			}
			if (vec[1]>0) {
				//targetDirection = Direction.up;
				TrySetMoveDirection(Direction.down);
				//currentDirection = Direction.down;
			}
			if (vec[1]<0) {
				//targetDirection = Direction.down;
				TrySetMoveDirection(Direction.up);
				//currentDirection = Direction.up;
			}
		}
	}

	//Checks if character needs to change direction (turn) in order to follow a path.
	//AI only. Simulates holding the key
	void CheckPath() {
		if (path.Count>0) {
			if (ComparePoints(currentPos, path[0])) {
				if (path.Count>2) {
					//int[] vec = new int[2]{path [0] [0] - currentPos [0], path [0] [1] - currentPos [1]};
					int[] vec = new int[2]{path [2] [0] - path [1] [0], path [2] [1] - path [1] [1]};
					if (ComparePoints(vec, new int[2]{1,0})) {
						//targetDirection = Direction.right;
						TrySetMoveDirection(Direction.right);
						//currentDirection = Direction.right;
					}
					if (ComparePoints(vec, new int[2]{-1,0})) {
						//targetDirection = Direction.left;
						TrySetMoveDirection(Direction.left);
						//currentDirection = Direction.left;
					}
					if (ComparePoints(vec, new int[2]{0,1})) {
						//targetDirection = Direction.up;
						TrySetMoveDirection(Direction.down);
						//currentDirection = Direction.down;
					}
					if (ComparePoints(vec, new int[2]{0,-1})) {
						//targetDirection = Direction.down;
						TrySetMoveDirection(Direction.up);
						//currentDirection = Direction.up;
					}
				}

				//Destination reached
				path.RemoveAt (0);
				if (path.Count==1) {
					if (gameObject.GetComponent<EnemyController>()!=null) {
						gameObject.GetComponent<EnemyController>().OnDestination();
					}
				}
			}
		}
	}

	//Moving cycle
	//Pretty messy
	void Move(Vector3 v) {
		float speed = 0;
		if (this.CompareTag("enemy")) {
			speed = GameManager.instance.enemySpeed;
		} else {
			speed = GameManager.instance.playerSpeed;
		}
		this.rigidbody.velocity = v * speed;
		int[] offs = GetGridOffset (currentDirection);

		//Distance that character will travel until the next FixedUpdate is called
		Vector3 dist = v * speed * Time.fixedDeltaTime;

		//If in the result of moving, character will surpass a strict int coordinate point
		//set new int coordinates and stop/change direction if needed
		Vector3 nextpos = this.transform.position + dist;

		//Calculate possible position in 3 physics cycles (needed for AI direction changing)
		Vector3 nextpos2x = this.transform.position + dist*3;
		if (dist.x<0) {
			if (nextpos.x<currentPos[0]+offs[0]+0.5f) {
				currentPos[0] = currentPos[0]+offs[0];
				availableDirections = GetAvailableDirections(currentPos[0], currentPos[1]);
				if (level[currentPos[0]+offs[0], currentPos[1]+offs[1]]=='a') {
					currentDirection = Direction.none;
				}
				if (targetDirection!=Direction.none) {
					if (availableDirections.Contains(targetDirection)) {
						currentDirection = targetDirection;
					}
				}
			}
			if (nextpos2x.x<currentPos[0]+offs[0]+0.5f) {
				CheckPath();
			}
			//This check is for a state, when player quickly changes direction to opposite without chaging current
			//coordinates.
			if (path.Count==0) {
				if (nextpos.x<currentPos[0]+0.5f) {
					if (level[currentPos[0]+offs[0], currentPos[1]+offs[1]]=='a') {
						currentDirection = Direction.none;
						availableDirections = GetAvailableDirections(currentPos[0], currentPos[1]);
					}
				}
			}
		}
		if (dist.x>0) {
			if (nextpos.x>currentPos[0]+offs[0]+0.5f) {
				currentPos[0] = currentPos[0]+offs[0];
				availableDirections = GetAvailableDirections(currentPos[0], currentPos[1]);
				if (level[currentPos[0]+offs[0], currentPos[1]+offs[1]]=='a') {
					currentDirection = Direction.none;
				}
				if (targetDirection!=Direction.none) {
					if (availableDirections.Contains(targetDirection)) {
						currentDirection = targetDirection;
					}
				}
			}
			if (nextpos2x.x>currentPos[0]+offs[0]+0.5f) {
				CheckPath();
			}
			//This check is for a state, when player quickly changes direction to opposite without chaging current
			//coordinates.
			if (path.Count==0) {
				if (nextpos.x>currentPos[0]+0.5f) {
					if (level[currentPos[0]+offs[0], currentPos[1]+offs[1]]=='a') {
						currentDirection = Direction.none;
						availableDirections = GetAvailableDirections(currentPos[0], currentPos[1]);
					}
				}
			}
		}
		if (dist.y>0) {
			if (level.GetLength(1) - nextpos.y<currentPos[1]+offs[1]+0.5f) {
				currentPos[1] = currentPos[1]+offs[1];
				availableDirections = GetAvailableDirections(currentPos[0], currentPos[1]);
				if (level[currentPos[0]+offs[0], currentPos[1]+offs[1]]=='a') {
					currentDirection = Direction.none;
				}
				if (targetDirection!=Direction.none) {
					if (availableDirections.Contains(targetDirection)) {
						currentDirection = targetDirection;
					}
				}
			}
			if (level.GetLength(1) - nextpos2x.y<currentPos[1]+offs[1]+0.5f) {
				CheckPath();
			}
			//This check is for a state, when player quickly changes direction to opposite without chaging current
			//coordinates.
			if (path.Count==0) {
				if (level.GetLength(1) - nextpos.y<currentPos[1]+0.5f) {
					if (level[currentPos[0]+offs[0], currentPos[1]+offs[1]]=='a') {
						currentDirection = Direction.none;
						availableDirections = GetAvailableDirections(currentPos[0], currentPos[1]);
					}
				}
			}
		}
		if (dist.y<0) {
			if (level.GetLength(1) - nextpos.y>currentPos[1]+offs[1]+0.5f) {
				currentPos[1] = currentPos[1]+offs[1];
				availableDirections = GetAvailableDirections(currentPos[0], currentPos[1]);
				if (level[currentPos[0]+offs[0], currentPos[1]+offs[1]]=='a') {
					currentDirection = Direction.none;
				}
				if (targetDirection!=Direction.none) {
					if (availableDirections.Contains(targetDirection)) {
						currentDirection = targetDirection;
					}
				}
			}
			if (level.GetLength(1) - nextpos2x.y>currentPos[1]+offs[1]+0.5f) {
				CheckPath();
			}
			//This check is for a state, when player quickly changes direction to opposite without chaging current
			//coordinates.
			if (path.Count==0) {
				if (level.GetLength(1) - nextpos.y>currentPos[1]+0.5f) {
					if (level[currentPos[0]+offs[0], currentPos[1]+offs[1]]=='a') {
						currentDirection = Direction.none;
						availableDirections = GetAvailableDirections(currentPos[0], currentPos[1]);
					}
				}
			}
		}
	}

	//Returns a list of available directions from certain point
	List<Direction> GetAvailableDirections(int i, int j) {
		List<Direction> list = new List<Direction> ();
		if (i>0) {
			if (level[i-1,j]!='a') {
				list.Add(Direction.left);
			}
		}
		if (i<level.GetLength(0)-1) {
			if (level[i+1,j]!='a') {
				list.Add(Direction.right);
			}
		}
		if (j>0) {
			if (level[i,j-1]!='a') {
				list.Add(Direction.up);
			}
		}
		if (j<level.GetLength(1)-1) {
			if (level[i,j+1]!='a') {
				list.Add(Direction.down);
			}
		}
		return list;
	}

	//Try to set the movement direction, if cant - 
	//set it as a target direction
	public void TrySetMoveDirection(Direction d) {
		/*
		int[] offs = GetGridOffset (d);
		if (level[currentPos[0]+offs[0], currentPos[1]+offs[1]]!='a') {
			currentDirection = d;
		}
		*/
		targetDirection = d;
		if (currentDirection==Direction.none) {
			if (availableDirections.Contains(d)) {
				currentDirection = d;
			}
		} else {
			if (currentDirection==Direction.left || currentDirection==Direction.right) {
				if (d==Direction.left || d==Direction.right) {
					currentDirection = d;
				}
			}
			if (currentDirection==Direction.up || currentDirection==Direction.down) {
				if (d==Direction.up || d==Direction.down) {
					currentDirection = d;
				}
			}
		}
	}

	int[] GetGridOffset(Direction d) {
		int[] offs = new int[2];
		switch (d) {
		case Direction.down: offs[0]=0; offs[1] = 1; break;
		case Direction.up: offs[0]=0; offs[1] = -1; break;
		case Direction.left: offs[0]=-1; offs[1] = 0; break;
		case Direction.right: offs[0]=1; offs[1] = 0; break;
		}
		return offs;
	}

	//Calculates a path to a destination point in a 2d grid.
	//Uses A* algorithm.
	List<int[]> CalculatePath(int[] dest) {
		int[] start = new int[2]{currentPos [0], currentPos [1]};
		List<PathNode> closed = new List<PathNode> ();
		List<PathNode> open = new List<PathNode> ();
		PathNode startNode = new PathNode (){
			point = start,
			cameFrom = null,
			g = 0,
			h = CalculateManhattan (start, dest)
		};
		open.Add (startNode);

		int iter = 0;

		while(open.Count>0) {
			PathNode current = FindLowestFNode(open);
			List<PathNode> neighbors = GetNeighbors(current, dest);
			if (ComparePoints(current.point, dest)) {
				//Shortest path found. Done.
				return ReconstructPath(current);
			}
			open.Remove(current);
			closed.Add(current);
			foreach (PathNode pn in neighbors) {
				//If the point is in the closed list - skip
				if (closed.Count(node=> ComparePoints(node.point, pn.point))>0) {
					continue;
				}
				PathNode openNode = open.FirstOrDefault(node => ComparePoints(node.point, pn.point));
				//If we havent calculated the node previously - add it to the open list
				if (openNode == null)
					open.Add(pn);
				else
				//Else, update the node if the path is shorter
				if (openNode.g > pn.g)
				{
					openNode.cameFrom = current;
					openNode.g = pn.g;
				}
			}
			iter++;
			if (iter>512) {
				break;
			}
		}
		//Search failed. Return nothing
		return null;
	}

	//Path reconstruction from nodes for A* algorithm
	List<int[]> ReconstructPath(PathNode goal) {
		List<int[]> result = new List<int[]>();
		PathNode currentNode = goal;
		while (currentNode!=null) {
			result.Add(currentNode.point);
			currentNode = currentNode.cameFrom;
		}
		result.Reverse ();
		return result;
	}

	//Manhattan distance heuristic function for A* algorithm
	int CalculateManhattan(int[] start, int[] dest) {
		return Mathf.Abs (start [0] - dest [0]) + Mathf.Abs (start [1] - dest [1]);
	}

	PathNode FindLowestFNode(List<PathNode> l) {
		PathNode lowest = l[0];
		foreach(PathNode pn in l) {
			if (pn.f<lowest.f) {
				lowest = pn;
			}
		}
		return lowest;
	}

	List<PathNode> GetNeighbors(PathNode current, int[] dest) {
		List<PathNode> l = new List<PathNode> ();
		List<int[]> neighborPos = new List<int[]> ();
		neighborPos.Add (new int[2]{current.point [0] + 1, current.point [1]});
		neighborPos.Add (new int[2]{current.point [0] - 1, current.point [1]});
		neighborPos.Add (new int[2]{current.point [0], current.point [1] + 1});
		neighborPos.Add (new int[2]{current.point [0], current.point [1] - 1});
		foreach (int[] np in neighborPos) {
			//Skip if outside the boundaries of a level
			if (np[0]<0 || np[0]>level.GetLength(0)-1) {
				continue;
			}
			if (np[1]<0 || np[1]>level.GetLength(1)-1) {
				continue;
			}
			//Skip if not walkable
			if (level[np[0], np[1]]=='a') {
				continue;
			}
			PathNode neighborNode = new PathNode()
			{
				point = np,
				cameFrom = current,
				g = current.g + 1,
				h = CalculateManhattan(np, dest),
			};
			l.Add(neighborNode);
		}
		return l;
	}

	bool ComparePoints(int[] p1, int[] p2) {
		if (p1[0]==p2[0] && p1[1]==p2[1]) {
			return true;
		} else {
			return false;
		}
	}

	public void Stop() {
		rigidbody.velocity = Vector3.zero;
		targetDirection = Direction.none;
		currentDirection = Direction.none;
		path = new List<int[]> ();
	}
}
