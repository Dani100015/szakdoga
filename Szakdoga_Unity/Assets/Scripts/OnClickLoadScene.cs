﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OnClickLoadScene : MonoBehaviour {

    public void LoadByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
	
}
