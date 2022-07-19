using System.Collections.Generic;
using UnityEngine;
using Speckle.Core.Credentials;
using System.Linq;
using Speckle.Core.Api;
using UnityEngine.Events;
using UnityEngine.UI;
using Stream = Speckle.Core.Api.Stream;
using Text = UnityEngine.UI.Text;

using Realms;
using System.Threading.Tasks;

namespace Speckle.ConnectorUnity
{
    public class SpeckleExamples : MonoBehaviour
  {
    public Text SelectStreamText;
    public Text DetailsStreamText;
    public Dropdown StreamSelectionDropdown;
    public Button AddReceiverBtn;
    public Toggle AutoReceiveToggle;
    public Button AddSenderBtn;
    public GameObject StreamPanel;
    public Canvas StreamsCanvas;

    private List<Stream> StreamList = null;
    private Stream SelectedStream = null;
    private List<GameObject> StreamPanels = new List<GameObject>();

    private Client client;
    

    async void Start()
    {

      // Check Internet Access
      if(Application.internetReachability == NetworkReachability.NotReachable)
      {
        Debug.Log("Error. Check internet connection!");
      }

      if (SelectStreamText == null || StreamSelectionDropdown == null)
      {
        Debug.Log("Please set all input fields on _SpeckleExamples");
        return;
      }

      var defaultAccount = new Account()
      {
        serverInfo = new ServerInfo
        {
          company = "RWTH Aachen",
          name = "Robots do what you see",
          url = "https://speckle.xyz"
        },
        token = "6bef49787348ec96369e89041b56f97fb0593f5f7f",
      };

      SelectStreamText.text = $"Select a stream on {defaultAccount.serverInfo.name}:";

      client = new Client(defaultAccount);
      
      StreamList = await client.StreamsGet(30);
      if (!StreamList.Any())
      {
        Debug.Log("There are no streams in your account, please create one online.");
        return;
      }

      StreamSelectionDropdown.options.Clear();
      foreach (var stream in StreamList)
      {
        StreamSelectionDropdown.options.Add(new Dropdown.OptionData(stream.name + " - " + stream.id));
      }

      StreamSelectionDropdown.onValueChanged.AddListener(StreamSelectionChanged);
      //trigger ui refresh, maybe there's a better method
      StreamSelectionDropdown.value = -1;
      StreamSelectionDropdown.value = 0;


      AddReceiverBtn.onClick.AddListener(AddReceiver);
      AddSenderBtn.onClick.AddListener(AddSender);
            
        }

    public void StreamSelectionChanged(int index)
    {
      if (index == -1)
        return;

      SelectedStream = StreamList[index];
      DetailsStreamText.text =
        $"Description: {SelectedStream.description}\n" +
        $"Link sharing on: {SelectedStream.isPublic}\n" +
        $"Role: {SelectedStream.role}\n" +
        $"Collaborators: {SelectedStream.collaborators.Count}\n" +
        $"Id: {SelectedStream.id}";
    }

    // Shows how to create a new Receiver from code and then pull data manually
    // Created receivers are added to a List of Receivers for future use
    private async void AddReceiver()
    {
      var autoReceive = AutoReceiveToggle.isOn;
      var stream = await client.StreamGet(SelectedStream.id, 30);

      var streamPrefab = Instantiate(StreamPanel, new Vector3(0, 0, 0),
        Quaternion.identity);

      //set position
      streamPrefab.transform.SetParent(StreamsCanvas.transform);
      var rt = streamPrefab.GetComponent<RectTransform>();
      rt.anchoredPosition = new Vector3(-10, -310 - StreamPanels.Count * 110, 0);

      streamPrefab.AddComponent<InteractionLogic>().InitReceiver(stream, autoReceive, client.Account);

      StreamPanels.Add(streamPrefab);
    }

    private async void AddSender()
    {
      var stream = await client.StreamGet(SelectedStream.id, 10);

      var streamPrefab = Instantiate(StreamPanel, new Vector3(0, 0, 0),
        Quaternion.identity);

      streamPrefab.transform.SetParent(StreamsCanvas.transform);
      var rt = streamPrefab.GetComponent<RectTransform>();
      rt.anchoredPosition = new Vector3(-10, -310 - StreamPanels.Count * 110, 0);

      streamPrefab.AddComponent<InteractionLogic>().InitSender(stream, client.Account);

      StreamPanels.Add(streamPrefab);
    }

    public void RemoveStreamPrefab(GameObject streamPrefab)
    {
      StreamPanels.RemoveAt(StreamPanels.FindIndex(x => x.name == streamPrefab.name));
      ReorderStreamPrefabs();
    }

    private void ReorderStreamPrefabs()
    {
      for (var i = 0; i < StreamPanels.Count; i++)
      {
        var rt = StreamPanels[i].GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector3(-10, -310 - i * 110, 0);
      }
    }
  }
}