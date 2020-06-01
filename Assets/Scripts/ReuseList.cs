using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct LayoutParam
{
    public float height;
    public float posY;
}

public class ReuseList : MonoBehaviour
{
    public GameObject titleTemp;    //title
    public GameObject itemTemp;    //复用item
    public GameObject content;
    public int lineCount = 7;       //每行显示最大个数
    public RectOffset padding;
    public Vector2 spacing;
    public bool testModel = false;
    private Rect viewSize;
    private Rect cellSize;
    private Rect titleSize;
    private Dictionary<int, LayoutParam> _layoutMap = new Dictionary<int, LayoutParam>();
    private Queue<GameObject> _unUsedQueue = new Queue<GameObject>();
    private Dictionary<string, GameObject> _usedMap = new Dictionary<string, GameObject>();
    private List<GameObject> _titleList = new List<GameObject>();
    private List<GameObject> _itemList = new List<GameObject>();
    private int[] CountArray;
    private bool changed = false;

    public delegate void VoidDelegate(int layoutIndex, int dataIndex, int realIndex, GameObject obj);
    public VoidDelegate onUpdateItem;
    public VoidDelegate onUpdateTitle;

    // Use this for initialization
    void Start()
    {
        viewSize = gameObject.GetComponent<RectTransform>().rect;
        cellSize = itemTemp.GetComponent<RectTransform>().rect;
        if (titleTemp != null)
        {
            titleSize = titleTemp.GetComponent<RectTransform>().rect;
        }

        RectTransform rt = content.GetComponent<RectTransform>();
        rt.anchorMin = new Vector3(0, 1);
        rt.anchorMax = new Vector3(0, 1);
        rt.pivot = new Vector3(0, 1);

        //测试代码
        if (testModel)
        {
            initList(new int[] { 4, 5, 6, 10, 20, 50, 100, 400 }, true);
        }
    }

    void initList(int[] array, bool isCreateTitle = false)
    {
        float H = padding.top;
        CountArray = array;
        for (int i = 0; i < CountArray.Length; i++)
        {
            if (isCreateTitle)
            {
                //创建title
                GameObject title;
                if (i >= _titleList.Count)
                {
                    title = (GameObject)GameObject.Instantiate(titleTemp);
                    title.transform.SetParent(content.transform);
                    title.transform.localScale = new Vector3(1f, 1f, 1f);

                    RectTransform rt = title.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector3(0, 1);
                    rt.anchorMax = new Vector3(0, 1);
                    rt.pivot = new Vector3(0, 1);

                    _titleList.Add(title);
                }
                else
                {
                    title = _titleList[i];
                }
                title.transform.localPosition = new Vector2(0, -H);
                title.SetActive(true);
                H += titleSize.height + spacing.y;

                if (onUpdateTitle != null)
                {
                    //int realIndex = _titleList.IndexOf(title);
                    onUpdateTitle(i, 0, 0, title);
                }
            }

            //创建layout参数
            int line = Mathf.CeilToInt(CountArray[i] * 1f / lineCount);
            LayoutParam layout = new LayoutParam();
            layout.posY = -H;
            layout.height = line * cellSize.height + (line - 1) * spacing.y;
            H += layout.height + spacing.y;

            _layoutMap.Add(i, layout);
        }

        content.GetComponent<RectTransform>().sizeDelta = new Vector2(viewSize.width, H + padding.bottom);

        checkLayout();
    }

    public void recyleUnusedItem()
    {
        Vector2 pos = content.transform.localPosition;

        List<string> keys = new List<string>();
        //超出屏幕的item    
        foreach (var v in _usedMap)
        {
            GameObject item = v.Value;
            float posY = item.transform.localPosition.y + pos.y;
            if (posY > cellSize.height || posY < -viewSize.height)
            {
                item.SetActive(false);
                _unUsedQueue.Enqueue(item);
                keys.Add(v.Key);
            }
        }

        foreach (var v in keys)
        {
            _usedMap.Remove(v);
        }
    }

    public void checkLayout()
    {
        Vector2 pos = content.transform.localPosition;
        foreach (var v in _layoutMap)
        {
            LayoutParam layout = v.Value;
            float h = layout.height;
            float posY1 = layout.posY + pos.y;
            float posY2 = layout.posY + pos.y - h;
            float start = 0f;
            float end = 0f;

            if (posY1 > 0 && posY2 <= 0 && posY2 >= -viewSize.height)             //顶部超出
            {
                start = posY1;
                end = h;
            }
            else if (posY1 <= 0 && posY1 > -viewSize.height && posY2 < -viewSize.height)    //底部超出
            {
                start = 0;
                end = viewSize.height + posY1;
            }
            else if (posY1 <= 0 && posY1 > -viewSize.height && posY2 < 0 && posY2 >= -viewSize.height)  //完全包含
            {
                start = 0;
                end = h;
            }
            else if (posY1 > 0 && posY2 < -viewSize.height)
            {
                start = posY1;
                end = posY1 + viewSize.height;
            }
            else
            {
                continue;
            }

            int _start = Mathf.FloorToInt(start / (cellSize.height + spacing.y));
            int _end = Mathf.FloorToInt(end / (cellSize.height + spacing.y));
            updateLayout(_start, _end, v.Key);
        }
    }

    void updateLayout(int start, int end, int index)
    {
        float posY = _layoutMap[index].posY;
        for (int i = start * lineCount; i < (end + 1) * lineCount; i++)
        {
            if (i >= CountArray[index]) { break; }
            string key = string.Format("{0}-{1}", index.ToString(), i.ToString());
            if (_usedMap.ContainsKey(key)) { continue; }

            GameObject item;
            if (_unUsedQueue.Count == 0)
            {
                item = (GameObject)GameObject.Instantiate(itemTemp);
                item.transform.SetParent(content.transform);
                item.transform.localScale = new Vector3(1f, 1f, 1f);

                RectTransform rt = item.GetComponent<RectTransform>();
                rt.anchorMin = new Vector3(0, 1);
                rt.anchorMax = new Vector3(0, 1);
                rt.pivot = new Vector3(0, 1);

                _itemList.Add(item);
            }
            else
            {
                item = _unUsedQueue.Dequeue();
            }
            item.transform.localPosition = new Vector2(padding.left + i % lineCount * (cellSize.width + spacing.x), posY - Mathf.FloorToInt(i * 1f / lineCount) * (cellSize.height + spacing.y));
            item.SetActive(true);
            _usedMap.Add(key, item);
            if (onUpdateItem != null)
            {
                int realIndex = _itemList.IndexOf(item);
                onUpdateItem(index, i, realIndex, item);
            }
        }
    }

    public void onValueChanged()
    {
        recyleUnusedItem();
        checkLayout();
    }

    //回收所有
    private void recyleAll()
    {
        foreach (var v in _usedMap)
        {
            GameObject item = v.Value;
            item.SetActive(false);
            _unUsedQueue.Enqueue(item);
        }
        _usedMap.Clear();

        foreach (var v in _titleList)
        {
            v.SetActive(false);
        }

        _layoutMap.Clear();
        content.transform.localPosition = new Vector2(0, 0);
    }
    public void ResetData(int[] array, bool isCreateTitle)
    {
        recyleAll();
        initList(array, isCreateTitle);
    }
}