using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DiaLogmanager : MonoBehaviour
{
    public UICameraMovment C1;
    
    public float mainCameraX;
    
    public Camera[] cameras; // 用于存储所有摄像机的数组
    private int currentCameraIndex = 0; // 当前激活的摄像机索引
    
    //对话内容文本，csv格式
    public TextAsset dialogDataFile;
    
    //左侧角色图像
    public SpriteRenderer spriteLeft;

    //右侧角色图像
    public SpriteRenderer spriteRight;

    //角色名字文本
    public TMP_Text nameText;

    //对话内容文本
    public TMP_Text dialogText;

    //角色图片列表
    public List<Sprite> sprites = new List<Sprite>();
    
    //背景图片列表
    public List<Sprite> _backgroundSprites = new List<Sprite>();

    //角色名字对应图片的字典
    Dictionary<string, Sprite> imageDic = new Dictionary<string, Sprite>();

    //当前对话索引值
    public int dialogIndex;

    //对话文本按行分割
    public string[] dialogRows;

    //继续按钮
    public Button next;

    //选项按钮
    public GameObject optionButton;

    //选项按钮父节点
    public Transform buttonGroup;
    
    //背景图片
    public SpriteRenderer backGround;
    
    //音乐播放
    private AudioManager musicManager;

    private Vector3 UIPosition;
    private Vector3 GamePosition;
    
    
    private void Awake()
    {
        musicManager = FindObjectOfType<AudioManager>();
        imageDic["爱丽儿无语"] = sprites[0];
        imageDic["爱丽儿平静"] = sprites[1];
        imageDic["爱丽儿星星眼"] = sprites[2];
        imageDic["爱丽儿笑"] = sprites[3];
        imageDic["爱丽儿迷失"] = sprites[4];
        imageDic["米兰达哭"] = sprites[5];
        imageDic["米兰达好奇"] = sprites[6];
        imageDic["米兰达平静"] = sprites[7];
        imageDic["米兰达思考"] = sprites[8];
        imageDic["米兰达笑"] = sprites[9];
        imageDic["帕克-交互前"] = sprites[10];
        imageDic["帕克-交互后"] = sprites[11];
        imageDic["豆花-交互前"] = sprites[12];
        imageDic["豆花-交互后"] = sprites[13];
        imageDic["菲比&罗宾-交互前"] = sprites[14];
        imageDic["菲比&罗宾-交互后"] = sprites[15];
        imageDic["米兰达（小）哭"] = sprites[16];
        imageDic["米兰达（小）平静"] = sprites[17];
        imageDic["米兰达（小）笑"] = sprites[18];
        imageDic["null"] = sprites[19];
        
    }

    void Start()
    {
        UIPosition = transform.position;
        SwitchCamera(1);
        ReadText(dialogDataFile);
        ShowDiaLogRow();
    }

    //更新文本信息
    public void UpdateText(string _name, string _text)
    {
        nameText.text = _name;
        dialogText.text = _text;
    }

    //更新图片信息
    public void UpdateImage(string _name, string _position)
    {
        if (_position == "左")
        {
            spriteLeft.sprite = imageDic[_name];
        }
        else if (_position == "右")
        {
            spriteRight.sprite = imageDic[_name];
        }
    }

    public void ReadText(TextAsset _textAsset)
    {
        dialogRows = _textAsset.text.Split('\n');//以换行来分割
        Debug.Log("读取成果");
    }
    
    public void ShowDiaLogRow()
    {
        for (int i = 0; i < dialogRows.Length; i++)
        {
            string[] cells = dialogRows[i].Split(',');
            
            if (cells[0] == "#" && int.Parse(cells[1]) == dialogIndex)
            {
                UpdateText(cells[2], cells[4]);
                UpdateImage(cells[8], cells[3]);

                dialogIndex = int.Parse(cells[5]);
                next.gameObject.SetActive(true);
                
                if (cells[6] != "")
                {
                    Debug.Log(cells[6]);
                    string[] effect = cells[6].Split('@');
                    OptionEffect(effect[0], int.Parse(effect[1]),cells[7]);
                }
                break;
            }
            else if (cells[0] == "&" && int.Parse(cells[1]) == dialogIndex)
            {
                next.gameObject.SetActive(false);//隐藏原来的按钮
                GenerateOption(i);
            }
            else if (cells[0] == "end" && int.Parse(cells[i]) == dialogIndex)
            {
                Debug.Log("剧情结束");//这里结束
            }
        }
    }

    public void OnClickNext()
    {
        ShowDiaLogRow();
    }

    public void GenerateOption(int _index)//生成按钮
    {
        string[] cells = dialogRows[_index].Split(',');
        if (cells[0] == "&")
        {
            GameObject button = Instantiate(optionButton, buttonGroup);
            //绑定按钮事件
            button.GetComponentInChildren<TMP_Text>().text = cells[4];
            button.GetComponent<Button>().onClick.AddListener
            (
                delegate 
                {
                    if (cells[6] != "")
                    {
                        string[] effect = cells[6].Split('@');
                        
                        OptionEffect(effect[0], int.Parse(effect[1]),cells[7]);
                    }

                    OnOptionClick(int.Parse(cells[5]));
                    
                });
            GenerateOption(_index + 1);
        }
    }

    public void OnOptionClick(int _id)
    {
        dialogIndex = _id;
        ShowDiaLogRow();
        for (int i = 0; i < buttonGroup.childCount; i++)
        {
            Destroy(buttonGroup.GetChild(i).gameObject);
        }
    }

    public void OptionEffect(string _effect, int _param, string _target)
    {
        switch (_effect)
        {
            case "背景切换" :
                backGround.sprite = _backgroundSprites[_param];
                break;
            case "转场" :
                Transition(_param);
                break;
            case "开始游戏" :
                SwitchCamera(0);
                C1.StartPosition();
                break;
            case "退场" :
                spriteLeft.sprite = sprites[19];
                spriteRight.sprite = sprites[19];
                break;
            case "音乐" :
                musicManager.PlayMusic(_param);
                break;
            case "进入关卡" :
                EnterGame();
                musicManager.PlayMusic(_param);
                break;
            case "开始游戏五" :
                SwitchCamera(0);
                break;
            case "开始游戏二" :
                SwitchCamera(0);
                break;
            case "结局" :
                backGround.sprite = _backgroundSprites[4];
                spriteLeft.sprite = sprites[19];
                spriteRight.sprite = sprites[19];
                break;
            case "重新开始" :
                SceneLoader.Instance.TransitionToScene("Game1");
                break;
            case "退出" :
                Application.Quit();
                break;
        }

    }

    public void EnterGame()
    {
        //C1.ToMainCamera();
        transform.position = new Vector3(mainCameraX, transform.position.y, transform.position.z);
        SwitchCamera(1);
    }

    public void Transition(int param)
    {
        cameras[currentCameraIndex].gameObject.SetActive(false);
        cameras[2].gameObject.SetActive(true);

        backGround.sprite = _backgroundSprites[param];
        Invoke("OpenLastCamera",1f);

    }
    
    
    void SwitchCamera(int index)
    {
        // 禁用当前摄像机
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(false);
        }
        // 启用目标摄像机
        cameras[index].gameObject.SetActive(true);
        // 更新当前摄像机索引
        currentCameraIndex = index;
    }

    public void OpenLastCamera()
    {
        cameras[2].gameObject.SetActive(false);
        cameras[currentCameraIndex].gameObject.SetActive(true);
    }

    public void Game2AfterButton()
    {
        SwitchCamera(1);
    }
    
    
}