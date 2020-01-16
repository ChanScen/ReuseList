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

    public int lineCount = 7;
    public float CellWidth = 258f;
    public float CellHeight = 352f;
    public float viewH = 380f;
    public float viewW = 380f;
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
        ResetData(new int[] { 2, 6, 8, 9, 20, 29, 40 }, true);
    }

    void initList(int[] array, bool isCrrateTitle = false)
    {
        float H = 0;
        CountArray = array;
        for (int i = 0; i < CountArray.Length; i++)
        {
            if (isCrrateTitle)
            {
                //创建title
                GameObject title;
                if (i >= _titleList.Count)
                {
                    title = (GameObject)GameObject.Instantiate(titleTemp);
                    title.transform.SetParent(content.transform);
                    title.transform.localScale = new Vector3(1f,1f,1f);
                    _titleList.Add(title);
                }
                else
                {
                    title = _titleList[i];
                }
                title.transform.localPosition = new Vector2(0, -H);
                title.SetActive(true);
                H += title.GetComponent<RectTransform>().sizeDelta.y;

                if(onUpdateTitle != null){
                    //int realIndex = _titleList.IndexOf(title);
                    onUpdateTitle(i,0,0,title);
                }
            }

            //创建layout参数
            int line = Mathf.CeilToInt(CountArray[i] * 1f / lineCount);
            LayoutParam layout = new LayoutParam();
            layout.posY = -H;
            layout.height = line * CellHeight;
            H += line * CellHeight;

            _layoutMap.Add(i, layout);
        }

        content.GetComponent<RectTransform>().sizeDelta = new Vector2(viewW, H+20);

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
            if (posY > CellHeight || posY < -viewH)
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

            if (posY1 > 0 && posY2 <= 0 && posY2 >= -viewH)             //顶部超出
            {
                start = posY1;
                end = h;
            }
            else if (posY1 <= 0 && posY1 > -viewH && posY2 < -viewH)    //底部超出
            {
                start = 0;
                end = viewH + posY1;
            }
            else if (posY1 <= 0 && posY1 > -viewH && posY2 < 0 && posY2 >= -viewH)  //完全包含
            {
                start = 0;
                end = h;
            }
            else if (posY1 > 0 && posY2 < -viewH)
            {
                start = posY1;
                end = posY1 + viewH;
            }
            else
            {
                continue;
            }

            int _start = Mathf.FloorToInt(start / CellHeight);
            int _end = Mathf.FloorToInt(end / CellHeight);
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
                item.transform.localScale = new Vector3(1f,1f,1f);
                _itemList.Add(item);
            }
            else
            {
                item = _unUsedQueue.Dequeue();
            }
            item.transform.localPosition = new Vector2(i % lineCount * CellWidth, posY - Mathf.FloorToInt(i * 1f / lineCount) * CellHeight);
            item.SetActive(true);
            _usedMap.Add(key, item);
            if(onUpdateItem != null){
                int realIndex = _itemList.IndexOf(item);
                onUpdateItem(index,i,realIndex,item);
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
        content.transform.localPosition = new Vector2(0,0);
    }
    public void ResetData(int[] array, bool isCrrateTitle)
    {
        recyleAll();
        initList(array, isCrrateTitle);
    }

    public void btnClick(){
        ResetData(new int[] {  9, 20 }, false);
    }
}
