﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 
/// </summary>
/// <typeparam name="TItem">List Item</typeparam>
/// <typeparam name="TData">List Item Data Clas</typeparam>
public abstract class ListView<TItem, TData> : MonoBehaviour
    where TItem : ListViewItem<TData>
{
    public List<TItem> items;
    public TItem listItemPrefab;
    public Transform contentView;
    public RectTransform contentsRect;

    private Vector2 listRectSize;
    private Vector2 itemRectSize;
    private int viewItemCount;
    private float spacing;

    /// <summary>
    /// Initialize List
    /// </summary>
    /// <param name="_items">list item data</param>
    public virtual IEnumerator Init(List<TData> _items)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            var itemView = Instantiate(listItemPrefab, contentView);

            itemView.Init(_items[i], i);

            var click = itemView.GetComponent<Button>();

            if (click == null)
            {
                click = itemView.gameObject.AddComponent<Button>();
            }

            click.onClick.AddListener(() =>
            {
                SelectItem(itemView.Index);
            });

            items.Add(itemView);

            yield return null;
        }

        listRectSize = GetComponent<RectTransform>().sizeDelta;
        itemRectSize = listItemPrefab.GetComponent<RectTransform>().sizeDelta;
        viewItemCount = Mathf.RoundToInt(listRectSize.y / itemRectSize.y);
        spacing = contentsRect.GetComponent<VerticalLayoutGroup>().spacing;
    }

    /// <summary>
    /// 리스트 아이템 클릭 시 처리
    /// </summary>
    /// <param name="_index">list item index</param>
    public abstract void SelectItem(int _index);


    /// <summary>
    /// 리스트 스크롤 위치 설정
    /// </summary>
    /// <param name="_targetIndex">target index</param>
    /// <param name="_aimated">scroll direction.(true -> vertical / false -> holizontal)</param>
    public IEnumerator SetScrollPosition(int _targetIndex, bool _aimated)
    {
        int viewItemCountHalf = viewItemCount / 2;
        int index = _targetIndex - viewItemCountHalf;

        if(_targetIndex <= viewItemCountHalf)
        {
            index = 0;
        }
        else if (_targetIndex >= items.Count - viewItemCountHalf)
        {
            index = items.Count - viewItemCountHalf;
        }

        float targetPosition = (itemRectSize.y * index) + (spacing * index);
        float currentPosition = 0;

        if(_aimated)
        {
            while (currentPosition < targetPosition)
            {
                currentPosition += itemRectSize.y;

                contentsRect.transform.localPosition = Vector3.up * currentPosition;
                yield return null;
            }
        }

        contentsRect.transform.localPosition = Vector3.up * targetPosition;
    }
}
