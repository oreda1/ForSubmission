using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ObjName
{
    Kettle2, Mug13, Pot, Paper7, Lamp1, Plate6, CanOld, CanOld_Damaged
}

public class Indicate_Item : MonoBehaviour
{
    private Player_Collision coll;
    [SerializeField] private List<Texture> _texture_list;
    [SerializeField] private RawImage[] item_raw;
    [SerializeField] private RawImage raw;
    [SerializeField] private Text text, nametext, count_text;
    [SerializeField] private TextMeshProUGUI now_count;
    [SerializeField] private Scrollbar scrollbar;
    [SerializeField] private GameObject box_parent;
    [SerializeField] private GameObject[] box;

    public event Action<int> OnLeftoverCountChanged;



    private Color base_color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    private Color receipt_color = Color.white;
    private bool box_flag;
    private int item_number = 0, leftover_count=8, texture_number = 0;
    
    private float itemDataCooldown = 10.0f, lastItemDataTime = -10.0f;
    private bool textureSet = false;

    void Start()
    {
        coll = GameObject.Find("Player").GetComponent<Player_Collision>();
        box_flag = gameObject.transform.GetChild(0).gameObject.activeInHierarchy;
        ResetUI();
      
      
    }

    void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.JoystickButton2))
        {
            gameObject.SetActive(true);
            ToggleBoxes();
            if (Time.time - lastItemDataTime >= itemDataCooldown)
            {
                Get_ItemData();
            }
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            scrollbar.value += 0.1f;
        }
    }

    private void ResetUI()
    {
        text.text = "";
        nametext.text = "";
        raw.texture = null;
        count_text.text = "残りのアイテム数  ";
        foreach (var b in box)
        {
            b.SetActive(false);
        }
    }

    private void ToggleBoxes()
    {
        foreach (var b in box)
        {
            b.SetActive(!box_flag);
        }
        box_flag = !box_flag;
    }

    private void Get_ItemData()
    {
        string itemName = coll.DisplayItemsInView();
        if (!Enum.TryParse(itemName, out ObjName itemEnum)) return;

        item_number = (int)itemEnum;
        if (raw.texture == null)
        {
            raw.texture = _texture_list[item_number];
            nametext.text = GetItemName(itemEnum);
            text.text = GetItemDescription(itemEnum);
            StartCoroutine(ClearItemData());
            Set_Texture();
            lastItemDataTime = Time.time;
        }
    }

    private string GetItemName(ObjName item)
    {
        return item switch
        {
            ObjName.Kettle2 => "ポッド",
            ObjName.Mug13 => "マグカップ",
            ObjName.Pot => "圧鍋",
            ObjName.Paper7 => "紙",
            ObjName.Lamp1 => "スタンドライト",
            ObjName.Plate6 => "お皿",
            ObjName.CanOld => "空き缶",
            ObjName.CanOld_Damaged => "潰れている空き缶",
            _ => ""
        };
    }

    private string GetItemDescription(ObjName item)
    {
        return item switch
        {
            ObjName.Kettle2 => "一般的なポッドだ",
            ObjName.Mug13 => "マグカップだ、、ポッドと合わせて使える",
            ObjName.Pot => "キッチンにあった圧鍋だ、、俺..料理下手なんだよなぁ",
            ObjName.Paper7 => "この紙は、、、何の紙なんだ?",
            ObjName.Lamp1 => "書斎にあったスタンドライトだ、、中の豆電球は何かに使えるかもしれない",
            ObjName.Plate6 => "キッチンにあった皿だ、、",
            ObjName.CanOld => "空き缶だ、、何か入っている",
            ObjName.CanOld_Damaged => "何も入っていない。",
            _ => ""
        };
    }

    private IEnumerator ClearItemData()
    {
        yield return new WaitForSeconds(5);
        raw.texture = null;
        text.text = "";
        nametext.text = "";
        count_text.text = "残りのアイテム数 ";
        now_count.text = ""+leftover_count;
        textureSet = false;
    }
 

    private void Set_Texture()
    {
        foreach (var item in item_raw)
        {
            if (item.texture == raw.texture) return;
        }

        if (raw.texture != null)
        {
            item_raw[texture_number].texture = raw.texture;
            item_raw[texture_number].color = Color.white;
            texture_number++;
            textureSet = true;
            UpdateLeftoverCount();
            DisableOutline();
        }
    }

    private void UpdateLeftoverCount()
    {
        leftover_count = Mathf.Max(0, leftover_count - 1);
        count_text.text = "残りのアイテム数 ";
        now_count.text = "" + leftover_count;

        // イベントを発火して、外部クラスに新しい値を渡す
        OnLeftoverCountChanged?.Invoke(leftover_count);
    }
    public int GetLeftoverCount
    {
        get { return leftover_count; }

        set { leftover_count = value; } 

    }



    private void DisableOutline()
    {
        
        GameObject foundItem = GameObject.Find(coll.itemName);
        if (foundItem?.GetComponent<Outline>() is { } itemOutline)
        {
            itemOutline.enabled = false;
        }
    }
}


