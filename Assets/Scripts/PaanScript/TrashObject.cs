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

    public void SetTrashObjectParent(ITrashObjectParent trashObjectParent)
    {
        if (this.trashObjectParent != null)
        {
            this.trashObjectParent.ClearTrashObject();
        }

        this.trashObjectParent = trashObjectParent;

        if (trashObjectParent.HasTrashObject())
        {
            Debug.LogError("ITrashObjectParent already has a TrashObject!");
        }

        trashObjectParent.SetTrashObject(this);

        transform.parent = trashObjectParent.GetTrashObjectFollowTransform();
        transform.localPosition = Vector3.zero;
    }

    public ITrashObjectParent GetTrashObjectParent()
    {
        return trashObjectParent;
    }

    public void DestroySelf()
    {
        trashObjectParent.ClearTrashObject();

        Destroy(gameObject);
    }

    public static TrashObject SpawnTrashObject(TrashObjectSO trashObjectSO, ITrashObjectParent trashObjectParent)
    {
        Transform trashObjectTransform = Instantiate(trashObjectSO.prefab);

        TrashObject trashObject = trashObjectTransform.GetComponent<TrashObject>();

        trashObject.SetTrashObjectParent(trashObjectParent);

        return trashObject;
    }
}
