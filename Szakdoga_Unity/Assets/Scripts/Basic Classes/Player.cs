﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player {

    string empireName;
    Species species;

    List<SolarSystem> playerSystems; //irányított naprendszerek
    List<Planets> playerColonizedPlanets; //kolonizáld player planéták

    List<Unit> units; //
    List<Player> allies;
    List<Player> enemies;

    int palladium;      //
    int iridium;        //Nyersanyagok
    int nullElement;    //

    int unitSize;       //Egységek
    int maxUnitSize;    //Max egységek

    //List<Unit>


    public Player()
    {

    }

}