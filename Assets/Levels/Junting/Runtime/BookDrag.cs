using UnityEngine;
using UnityEngine.EventSystems;
namespace XunJiYiDa.Levels.Junting {
 public class BookDrag:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerClickHandler {
  public JuntingGame game; public int book; RectTransform rect;Vector3 origin;int index;bool moved;CanvasGroup group;
  public void OnPointerClick(PointerEventData e){if(!moved)game.SelectBook(book);moved=false;}
  public void OnBeginDrag(PointerEventData e){if(!game.CanPlay)return;rect=GetComponent<RectTransform>();group=GetComponent<CanvasGroup>()??gameObject.AddComponent<CanvasGroup>();origin=rect.position;index=rect.GetSiblingIndex();rect.SetAsLastSibling();group.blocksRaycasts=false;moved=true;game.SelectBook(book);}
  public void OnDrag(PointerEventData e){if(!game.CanPlay||rect==null)return;Vector3 pos;if(RectTransformUtility.ScreenPointToWorldPointInRectangle(rect.parent as RectTransform,e.position,e.pressEventCamera,out pos))rect.position=pos;}
  public void OnEndDrag(PointerEventData e){if(rect==null)return;rect.position=origin;rect.SetSiblingIndex(index);if(group)group.blocksRaycasts=true;if(game.CanPlay)game.DropBook(book,e.position,e.pressEventCamera);rect=null;}
 }
 public class HoverSlot:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler {
  public JuntingGame game;public int slot;
  public void OnPointerEnter(PointerEventData e){game.HighlightSlot(slot,true);}
  public void OnPointerExit(PointerEventData e){game.HighlightSlot(slot,false);}
 }
}
