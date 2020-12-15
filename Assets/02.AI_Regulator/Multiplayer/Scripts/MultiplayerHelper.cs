using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{

    public enum MultiplayerMenu { main, loading, lobby };

    public enum DisconnectionType { left, kick, gameVersion, timeOut, abort }

    public enum InputMode
    {
        none,
        spawnFaction,
        create,
        customCommand,
        destroy,
        unitGroup,
        multipleAttack,
        research,
        unitEscape,
        self,
        movement,
        factionEntity,
        unit,
        building,
        resource,
        collect,
        dropoff,
        faction,
        attack,
        portal,
        upgrade,
        APC,
        APCEject,
        APCEjectAll,
        heal,
        convertOrder,
        convert,
        builder,
        health,
    }; //allowed types of the input's target.

    //these are the attributes that an input can have.
    public struct NetworkInput
    {
        public int factionID; //input player's faction ID

        public byte sourceMode; //input source's mode
        public byte targetMode; //input's target mode

        public int sourceID; //object that launched the command
        public string groupSourceID; //a string that holds a group of unit sources

        public int targetID; //target object that will get the command

        public Vector3 initialPosition; //initial position of the source obj
        public Vector3 targetPosition; //target position

        public int value; //extra int attribute
    }
}