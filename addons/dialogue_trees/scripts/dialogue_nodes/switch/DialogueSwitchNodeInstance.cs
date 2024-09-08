using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees;

public partial class DialogueSwitchNodeInstance : DialogueNodeInstance
{
    public string[] CaseTexts;

    public override void Ready(DialogueNodeSaveData data)
    {
        CaseTexts = data.General[0].AsStringArray();
    }
    
    public override void RecieveDialogueInput(string input, Variant[] parameters)
    {   
        for(int x = 0; x < CaseTexts.Length; x++)
        {
            RegEx regex = new ();
            regex.Compile($"(?i){CaseTexts[x]}");
            
            if(regex.Search(input) != null)
            {
                SendPortOutput(x);
                return;
            }   
        }

        SendPortOutput(CaseTexts.Length);
    }   

    public override DialogueInputOption[] GetDialogueInputOptions()
    {
        return DialogueInputOption.ConstructInputOptions(CaseTexts);
    }
}
