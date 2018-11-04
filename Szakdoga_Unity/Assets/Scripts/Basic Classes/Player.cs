﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player {

    public string empireName;
    ArrayList structures;
    public Species species;

    List<SolarSystem> playerSystems; //irányított naprendszerek
    List<Planets> playerColonizedPlanets; //kolonizált player planéták

    public List<GameObject> units; //
    public List<Player> allies;
    public List<Player> enemies;

    public int palladium;      //
    public int iridium;        //Nyersanyagok
    public int nullElement;    //

    int Population;       //Egységek
    int maxPopulation;    //Max egységek

    //Egységek importálása
    public List<GameObject> BuildableUnits = new List<GameObject>();
    public List<Tech> ResearchableTechs = new List<Tech>();

    public Player(int pall, int irid, int eezo, string name, Species spec)
    {
        palladium = pall;
        iridium = irid;
        nullElement = eezo;
        empireName = name;
        species = spec;
        units = new List<GameObject>();
        allies = new List<Player>();
        enemies = new List<Player>();
    }

    public int Palladium
    {
        get
        {
            return palladium;
        }
        set
        {
            palladium = value;
        }
    }
    public int Iridium
    {
        get
        {
            return iridium;
        }
        set
        {
            iridium = value;
        }
    }
    public int NullElement
    {
        get
        {
            return nullElement;
        }
        set
        {
            nullElement = value;
        }
    }
}