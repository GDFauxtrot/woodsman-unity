﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitParent : MonoBehaviour {

	private List<GameObject> children = new List<GameObject>();
	private bool isSelected;
	public Unit unit;

	private bool canBeSelected;
	private bool areTreesSick;

	private Material matMain;
	private Material matSelected;
	private Material matTMain;
	private Material matTSelected;
	private Material matGrowing;

	private GameData data;

	private float growbackFactor;

	void Start () {
		data = GameObject.Find("GameHandler").GetComponent<GameData>();
		isSelected = false;
		canBeSelected = true;
		growbackFactor = 1f;
		areTreesSick = false;

		matMain = (Material)Resources.Load("Tree-Main");
		matSelected = (Material)Resources.Load("Tree-Selected");
		matTMain = (Material)Resources.Load("Tree-TrunkMain");
		matTSelected = (Material)Resources.Load("Tree-TrunkSelected");
		matGrowing = (Material)Resources.Load("Tree-Growing");
	}

	public void setSelected(bool b) {
		isSelected = b;

		if (isSelected) {
			changeMaterials(matSelected, matTSelected);
		} else {
			changeMaterials(matMain, matTMain);
		}
	}

	public bool getSelected() {
		return isSelected;
	}

	private void changeMaterials(Material branchMaterial, Material trunkMaterial) {
		foreach (GameObject go in this.children) {
			foreach (Transform child in go.transform) {
				if (child.name == "Branches") {
					foreach (Transform branchChild in child) {
						branchChild.gameObject.GetComponent<Renderer>().sharedMaterial = branchMaterial;
					}
				} else {
					child.gameObject.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
				}
			}
		}
	}

	public void AddChild(GameObject go) {
		children.Add(go);
	}

	public List<GameObject> GetChildren() {
		return children;
	}

	public void CutDownUnit(float cutTime) {
		setSelected(false);
		StartCoroutine(RespawnTrees(cutTime, 1 / (growbackFactor * 4f * 5f)));
	}

	void Update() {
		if (canBeSelected && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) {
			if (Camera.main.GetComponent<CameraController>().canClick && Input.GetMouseButton(0)) {
				var camx = Camera.main.GetComponent<CameraController>().point.x;
				var camy = Camera.main.GetComponent<CameraController>().point.z;
				var px = transform.position.x;
				var py = transform.position.z;
				if (camx >= px && camx < (px + unit.getSize().x) &&
					camy >= py && camy < (py + unit.getSize().y)) {
					if (camx >= 0f && camx < 100f && camy >= 0f && camy < 100f && !isSelected) {
						setSelected(true);
						data.AddParentToSelected(GetComponent<UnitParent>());
					}
				} else {
					if (!Input.GetKey(KeyCode.LeftShift) && isSelected) {
						setSelected(false);
						data.RemoveParentFromSelected(GetComponent<UnitParent>());
						// When clicking on a tree then clicking on BG trees,
						// the UI isn't reset. This coroutine waits a frame
						// and checks if the selection count is 0, and if so,
						// calls the reset function.
						StartCoroutine(CheckSelected());
					}
				}
			}
		}
	}

	private IEnumerator CheckSelected() {
		// Data passed around will take some time to settle, so wait a frame
		yield return new WaitForSeconds(Time.deltaTime);
		if (data.GetSelectedParentsAsList().Count == 0) {
			data.ResetSelectedParents();
		}
	}

	private IEnumerator RespawnTrees(float wait, float speed) {
		canBeSelected = false;
		foreach (GameObject go in children) {
			go.isStatic = false;
			go.transform.localScale = Vector3.zero;
			changeMaterials(matGrowing, matGrowing);
		}
		yield return new WaitForSeconds(wait);

		if (wait == GameData.workerCutTime) {
			if (growbackFactor > 1f) {
				growbackFactor -= 0.35f;
			} else if (growbackFactor > 0.5f) {
				growbackFactor -= 0.17f;
			}
			data.SetWorkerCount(data.GetWorkerCount() + 1);
		} else if (wait == GameData.machineCutTime) {
			if (growbackFactor < 2f) {
				growbackFactor = Mathf.Lerp(growbackFactor, 2f, 0.5f);
			}
			data.SetMachineCount(data.GetMachineCount() + 1);
		} else if (wait == GameData.teamCutTime) {
			if (growbackFactor < 1.5f) {
				growbackFactor *= 2f;
			} else {
				growbackFactor = 3f;
			}
			data.SetTeamCount(data.GetTeamCount() + 1);
		}

		float scale = 0f;
		while (scale < 1f) {
			foreach (GameObject go in children) {
				go.transform.localScale = Vector3.one * scale;
			}
			scale += speed;
			yield return new WaitForSeconds(0.25f);
		}
		foreach (GameObject go in children) {
			go.isStatic = true;
		}
		canBeSelected = true;
		setSelected(false);
	}
}
