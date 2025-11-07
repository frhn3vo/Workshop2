using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashObject : MonoBehaviour
{
    [SerializeField] private TrashObjectSO trashObjectSO;

    private ITrashObjectParent trashObjectParent;

    public TrashObjectSO GetTrashObjectSO()
    {
        return trashObjectSO;
    }
}
