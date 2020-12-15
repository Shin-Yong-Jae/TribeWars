using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

public class MenuItems : MonoBehaviour {

	[MenuItem("RTS Engine/Configure New Map", false, 51)]
	private static void ConfigNewMapOption()
	{
		GameObject newMap = Instantiate(Resources.Load("NewMap", typeof(GameObject))) as GameObject;

        newMap.transform.DetachChildren();

        DestroyImmediate (newMap);

        print("Please set up the factions in order to fully configure the new map: http://soumidelrio.com/docs/unity-rts-engine/getting-started-create-a-new-map/");
	}

	[MenuItem("RTS Engine/Single Player Menu", false, 101)]
	private static void SinglePlayerMenuOption()
	{
		GameObject singlePlayerMenu = Instantiate(Resources.Load("SinglePlayerMenu", typeof(GameObject))) as GameObject;

        singlePlayerMenu.transform.DetachChildren();

        DestroyImmediate (singlePlayerMenu);
	}

#if RTSENGINE_MIRROR
    [MenuItem("RTS Engine/Multiplayer Menu (Mirror)", false, 102)]
	private static void MultiplayerMenuMenu_Mirror()
	{
        GameObject multiplayerMenu_Mirror = Instantiate(Resources.Load("MultiplayerMenu_Mirror", typeof(GameObject))) as GameObject;

        multiplayerMenu_Mirror.transform.DetachChildren();

		DestroyImmediate (multiplayerMenu_Mirror);
	}
#endif

    [MenuItem("RTS Engine/New Unit", false, 151)]
    private static void NewUnitOption()
    {
        Instantiate(Resources.Load("NewUnit", typeof(GameObject)));
    }

    [MenuItem("RTS Engine/New Building", false, 152)]
    private static void NewBuildingOption()
    {
        Instantiate(Resources.Load("NewBuilding", typeof(GameObject)));
    }

    [MenuItem("RTS Engine/New Resource", false, 153)]
    private static void NewResourceOption()
    {
        Instantiate(Resources.Load("NewResource", typeof(GameObject)));
    }

    [MenuItem("RTS Engine/New Attack Object", false, 154)]
    private static void NewAttackObject()
    {
        Instantiate(Resources.Load("NewAttackObject", typeof(GameObject)));
    }

    [MenuItem("RTS Engine/New NPC Manager", false, 155)]
    private static void NewNPCManager()
    {
        Instantiate(Resources.Load("NewNPCManager", typeof(GameObject)));
    }

    [MenuItem("RTS Engine/Documentation", false, 201)]
    private static void DocOption()
    {
        Application.OpenURL("http://soumidelrio.com/docs/unity-rts-engine/");
    }
    [MenuItem("RTS Engine/Review", false, 202)]
    private static void ReviewOption()
    {
        Application.OpenURL("https://assetstore.unity.com/packages/templates/packs/rts-engine-79732");
    }
}
