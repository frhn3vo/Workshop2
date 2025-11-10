using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITrashObjectParent
{
    public Transform GetTrashObjectFollowTransform();

    public void SetTrashObject(TrashObject trashObject);

    public TrashObject GetTrashObject();

    public void ClearTrashObject();

    public bool HasTrashObject();
}
