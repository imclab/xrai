using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FERM_Example_RollABall : MonoBehaviour {

    private const int nWalls = 3;
    private const float lerpTime = 1.5f;

    public Rigidbody bal;
    public Rigidbody box;
    public Transform hole;
    public BoxCollider[] walls;

    public Level[] levels;

    private int level;

    [Serializable]
    public struct Level {
        public Vector3 balStart;
        public Vector3[] boxPos;
        public Vector3[] boxSizes;
        public Vector3 holePos;
    }

    private void Start() {
        level = 0;
        SetLevel(levels[level]);
    }

    public void Update() {
        float y = Input.GetAxis("Vertical");
        float x = Input.GetAxis("Horizontal");
        Vector3 eul = 10f * new Vector3(y, 0f, -x);
        Quaternion rot = Quaternion.Euler(eul);

        box.rotation = Quaternion.Slerp(box.rotation, rot, 5f * Time.deltaTime);

        if(bal.position.y < -5f)
            NextLevel();
    }

    private void NextLevel() {
        if(++level >= levels.Length)
            level = 0;
        StartCoroutine("LerpLevel");
    }

    private IEnumerator LerpLevel() {
        bal.isKinematic = true;
        float time = 0f;

        while(time < lerpTime) {
            time += Time.deltaTime;
            LerpLevel(levels[level], 5f * Time.deltaTime);
            yield return null;
        }
        SetLevel(levels[level]);
        bal.isKinematic = false;
    }

    private void LerpLevel(Level level, float f) {
        bal.position = Vector3.Lerp(bal.position, level.balStart, f);
        hole.transform.localPosition = Vector3.Lerp(hole.transform.localPosition, level.holePos, f);

        for(int i = 0; i < walls.Length; i++) {
            Vector3 p = Vector3.Lerp(walls[i].transform.position, level.boxPos[i], f);
            Vector3 s = Vector3.Lerp(walls[i].size, level.boxSizes[i], f);
            walls[i].transform.position = p;
            walls[i].size = s;
            walls[i].GetComponent<FERM_Component>().SetParam("size", s);
        }
    }

    private void SetLevel(Level level) {
        bal.position = level.balStart;
        hole.transform.localPosition = level.holePos;

        for(int i = 0; i < walls.Length; i++) {
            walls[i].transform.position = level.boxPos[i];
            walls[i].size = level.boxSizes[i];
            walls[i].GetComponent<FERM_Component>().SetParam("size", level.boxSizes[i]);
        }
    }

}
