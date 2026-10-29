using System;
using NIU.Junting;
public static class RulesTests {
 static int count;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;}
 public static void Main(){
  var shelf=new ShelfRound();Check(!shelf.Place(5,1),"Wrong book rejected");Check(shelf.Placed==0,"Wrong placement preserves progress");Check(!shelf.Place(-1,1)&&!shelf.Place(1,6),"Invalid indices rejected");Check(shelf.Place(1,1)&&shelf.Place(3,3)&&shelf.Place(5,5),"Three books complete");Check(shelf.Placed==3&&!shelf.Place(1,1),"No double completion");
  Check(Book.Compare(new Book{classification="25.4",author="1234",year=2024},new Book{classification="50",author="0001",year=2010})<0,"Numeric classification order");Check(Book.Compare(new Book{classification="25.4",author="1000",year=2024},new Book{classification="25.4",author="2000",year=2000})<0,"Author order");Check(Book.Compare(new Book{classification="25.4",author="1000",year=2020},new Book{classification="25.4",author="1000",year=2024})<0,"Year order");
  var board=new DeerBoard();for(int x=1;x<=5;x++)Check(board.Toggle(x,0),"First five barriers accepted");Check(!board.Toggle(6,0)&&board.barriers.Count==5,"Sixth barrier rejected");Check(board.Toggle(1,0)&&board.barriers.Count==4,"Barrier removal works");Check(!board.Toggle(0,1)&&!board.Toggle(8,4),"Spawn and destination protected");Check(!board.Toggle(-1,0)&&!board.Toggle(12,0),"Out of bounds placement rejected");
  board=new DeerBoard();board.Toggle(9,1);board.released=true;Check(!board.Toggle(4,1),"Placement locked after release");for(int i=0;i<40;i++)board.Step();Check(board.arrived==3&&!board.escaped,"Known winning route reaches destination for three deer");board.Step();Check(board.arrived==3,"Arrivals are idempotent");
  board=new DeerBoard{released=true};for(int i=0;i<20;i++)board.Step();Check(board.escaped&&board.arrived==0,"Empty route loses");
  board=new DeerBoard();board.Toggle(9,1);board.Step();Check(board.ticks==0,"Deer cannot move before release");
  var round=new RoundState{stage=RoundState.Stage.Playing};round.Tick(1);round.paused=true;round.Tick(9);Check(round.remaining==59,"Pause freezes timer");round.paused=false;round.stage=RoundState.Stage.Quiz;round.Tick(20);Check(round.remaining==59,"Quiz freezes timer");round.stage=RoundState.Stage.Reward;round.Tick(20);Check(round.remaining==59,"Reward freezes timer");round.stage=RoundState.Stage.Playing;round.Tick(100);Check(round.remaining==0&&round.stage==RoundState.Stage.Failed,"Timeout saturates at zero and ends round");
  var score=new DemoScoreProvider().Evaluate("test","books",10.8f,2,1);Check(score.baseScore==300&&score.timeBonus==20&&score.mistakePenalty==40&&score.totalScore==280,"External scoring demo has correct breakdown");score=new DemoScoreProvider().Evaluate("test","books",0,99,0);Check(score.totalScore==0,"Score floor is zero");
  bool booksFirst=false,deerFirst=false,validOrders=true;
  for(int seed=0;seed<100;seed++){var route=new RandomLevelOrder(new[]{"books","deer"},new Random(seed));string a=route.DrawNext(),b=route.DrawNext();booksFirst|=a=="books";deerFirst|=a=="deer";validOrders&=a!=b&&route.Remaining==0&&route.DrawNext()==null;}
  Check(booksFirst&&deerFirst,"Both random orders occur across deterministic seeds");Check(validOrders,"Every route has exactly two different assignments and ends");
  var assigned=new RandomLevelOrder(new[]{"books","deer"},new Random(7));assigned.Exclude("books");Check(assigned.DrawNext()=="deer"&&assigned.DrawNext()==null,"Host assigned level excluded from remaining demo route");
  bool rejectsDuplicate=false;try{new RandomLevelOrder(new[]{"books","books"},new Random(0));}catch(ArgumentException){rejectsDuplicate=true;}Check(rejectsDuplicate,"Duplicate pool entries rejected");
  Console.WriteLine("PASSED "+count+" game-rule checks");
 }
}
