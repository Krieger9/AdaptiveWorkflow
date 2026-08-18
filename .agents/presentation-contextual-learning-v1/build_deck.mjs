import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const artifactEntry = "C:/Users/ghass/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";
const { Presentation, PresentationFile } = await import(pathToFileURL(artifactEntry).href);

const OUT = "D:/Projects/AdaptiveWorkflow/.agents/presentation-contextual-learning-v1/output";
const FINAL = "D:/Projects/AdaptiveWorkflow/Adaptive-Workflow-Contextual-Learning-v1.pptx";
const W = 1280, H = 720;
const C = { ink:"#101216", muted:"#5F6875", faint:"#8D96A3", line:"#DCE1E7", panel:"#F3F5F7", blue:"#3D8DFF", cyan:"#6DCBF4", pale:"#EAF6FD", green:"#38A169", amber:"#E59A23", red:"#D85B5B", white:"#FFFFFF" };

function shape(slide, geometry, x, y, w, h, fill=C.white, line=C.line, radius) {
  const s = slide.shapes.add({geometry, position:{left:x,top:y,width:w,height:h}, fill, line:{style:"solid",fill:line,width:line==="none"?0:1}, ...(radius?{borderRadius:radius}:{})});
  return s;
}
function txt(slide, text, x, y, w, h, size=22, color=C.ink, bold=false, align="left") {
  const s=slide.shapes.add({geometry:"textbox",position:{left:x,top:y,width:w,height:h},fill:"none",line:{style:"solid",fill:"none",width:0}});
  s.text=text; s.text.style={fontSize:size,fontFamily:"Aptos",color,bold,alignment:align,verticalAlignment:"middle",wrap:true}; return s;
}
function line(slide,x,y,w,h,color=C.line,width=2){ return slide.shapes.add({geometry:"line",position:{left:x,top:y,width:w,height:h},fill:"none",line:{style:"solid",fill:color,width}}); }
function pill(slide,text,x,y,w,fill=C.pale,color=C.blue){ shape(slide,"roundRect",x,y,w,30,fill,"none","rounded-xl"); txt(slide,text,x+10,y,w-20,30,12,color,true,"center"); }
function header(slide,n,title,kicker){
  slide.background.fill=C.white;
  txt(slide,String(n).padStart(2,"0"),64,35,44,28,13,C.blue,true);
  if(kicker) txt(slide,kicker.toUpperCase(),116,35,700,28,12,C.faint,true);
  txt(slide,title,64,73,1152,90,34,C.ink,true);
  line(slide,64,171,1152,0,C.line,1);
}
function footer(slide,n){ txt(slide,"ADAPTIVE WORKFLOW  /  CONTEXTUAL LEARNING",64,682,500,18,9,C.faint,true); txt(slide,String(n),1160,682,56,18,9,C.faint,true,"right"); }
function notes(slide,body,sources=[]){
  const sourceBlock=sources.length?`\n\n[Sources]\n${sources.map(s=>`- ${s}`).join("\n")}\n[/Sources]`:"";
  slide.speakerNotes.textFrame.setText(body+sourceBlock); slide.speakerNotes.setVisible(true);
}
function card(slide,x,y,w,h,title,body,accent=C.blue){ shape(slide,"roundRect",x,y,w,h,C.panel,"none","rounded-xl"); shape(slide,"rect",x,y,6,h,accent,"none"); txt(slide,title,x+24,y+18,w-42,34,18,C.ink,true); txt(slide,body,x+24,y+58,w-42,h-72,15,C.muted,false); }
function step(slide,x,y,w,num,title,body,color=C.blue){ shape(slide,"roundRect",x,y,w,150,C.white,C.line,"rounded-xl"); shape(slide,"ellipse",x+18,y+18,34,34,color,"none"); txt(slide,String(num),x+18,y+18,34,34,14,C.white,true,"center"); txt(slide,title,x+65,y+16,w-82,38,17,C.ink,true); txt(slide,body,x+18,y+62,w-36,72,14,C.muted); }
function addArrow(slide,x,y,w,color=C.blue){ shape(slide,"rightArrow",x,y,w,24,color,"none"); }

const p=Presentation.create({slideSize:{width:W,height:H}});

// 1 — title
{
 const s=p.slides.add(); s.background.fill=C.white;
 shape(s,"rect",0,0,18,H,C.blue,"none");
 pill(s,"SYSTEM THEORY",72,64,140);
 txt(s,"Contextual Learning\nfor Adaptive Applications",72,132,760,176,46,C.ink,true);
 txt(s,"How observed behavior becomes evidence, durable beliefs, and controlled application changes",76,332,700,92,23,C.muted);
 shape(s,"roundRect",858,92,330,460,C.panel,"none","rounded-xl");
 const labels=["Observe","Interpret","Learn","Adapt"];
 labels.forEach((v,i)=>{ const y=132+i*94; shape(s,"ellipse",894,y,48,48,i===3?C.blue:C.white,i===3?"none":C.line); txt(s,String(i+1),894,y,48,48,16,i===3?C.white:C.blue,true,"center"); txt(s,v,964,y,170,48,20,C.ink,true); if(i<3) line(s,918,y+48,0,46,C.line,2); });
 txt(s,"Theory, operating model, and current proof of concept",76,612,720,28,15,C.faint,true);
 notes(s,"Open by separating this deck from the live demonstration. This presentation explains the operating model: what the system observes, how it reasons, what it remembers, and how that becomes a controlled application behavior.");
}

// 2
{
 const s=p.slides.add(); header(s,2,"The model separates observations, evidence, beliefs, and adaptations","Core model");
 const xs=[64,354,644,934], titles=["OBSERVATION","EVIDENCE","BELIEF","ADAPTATION"], bodies=["A user action occurred in a known application context.","A pattern or relationship is supported by one or more episodes.","The system’s current, revisable explanation of the user.","A bounded change the application knows how to apply."];
 xs.forEach((x,i)=>{shape(s,"roundRect",x,230,230,250,i===2?C.pale:C.panel,"none","rounded-xl"); txt(s,titles[i],x+20,252,190,26,12,i===2?C.blue:C.faint,true); txt(s,bodies[i],x+20,302,190,118,18,C.ink,i===2); if(i<3)addArrow(s,x+240,338,40,C.line);});
 txt(s,"The discipline matters: an action is not automatically a preference, and a preference is not automatically permission to change the interface.",96,520,1088,72,23,C.ink,true,"center");
 footer(s,2); notes(s,"Emphasize the separation of concerns. This keeps the system from treating every click as truth or letting an AI response directly manipulate the interface.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilder.Data/Entities/Interaction.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilder.Data/Entities/Belief.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/AdaptationApprovalPolicy.cs"]);
}

// 3
{
 const s=p.slides.add(); header(s,3,"The application supplies meaning the AI cannot infer alone","Context");
 card(s,64,214,330,310,"Application identity","Which product and business domain?\n\nWhich surface and workflow step?\n\nWhat does the user believe they are doing?",C.blue);
 card(s,475,214,330,310,"Visible state","Which view is active?\n\nWhat controls and options are visible?\n\nWhich entities and values are present?",C.cyan);
 card(s,886,214,330,310,"Interaction semantics","What does expand, select, compare, or change-view mean here?\n\nWhich actions are reversible or consequential?",C.green);
 txt(s,"Without this contract, the model sees clicks. With it, the model can reason about intent.",160,562,960,48,24,C.ink,true,"center");
 footer(s,3); notes(s,"Context is designed into the application. The same gesture can mean different things on different surfaces, so the app identifies itself, describes the current workflow, supplies visible data, and defines the semantics of interactions.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderUI/src/collaboration/assembleSurfaceContext.ts","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderUI/src/collaboration/assembleSelectContractContext.ts","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/CollaborationContextFormatter.cs"]);
}

// 4
{
 const s=p.slides.add(); header(s,4,"Individual UI events are assembled into decision episodes","Event accumulation");
 const labels=["Expand A","Collapse A","Expand B","Choose graph","Select contract"];
 labels.forEach((v,i)=>{const x=68+i*226; shape(s,"roundRect",x,230,186,62,i===4?C.blue:C.panel,"none","rounded-xl"); txt(s,v,x+10,230,166,62,15,i===4?C.white:C.ink,true,"center"); if(i<4)addArrow(s,x+188,249,34,C.line);});
 shape(s,"roundRect",148,354,984,132,C.pale,"none","rounded-xl"); txt(s,"DECISION EPISODE",178,372,200,26,12,C.blue,true); txt(s,"The buffered exploration, reversals, timing, visible alternatives, and final action are submitted together.",178,409,900,50,21,C.ink,true);
 txt(s,"A coherent episode is more informative than five disconnected events.",188,535,904,40,22,C.muted,false,"center");
 footer(s,4); notes(s,"The UI does not ask the AI to interpret each micro-event independently. Exploration is buffered, then flushed at a meaningful boundary such as a view change or final selection. That gives the system a decision episode.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderUI/src/collaboration/observationBuffer.ts","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderUI/src/collaboration/useSurfaceObservations.ts"]);
}

// 5
{
 const s=p.slides.add(); header(s,5,"Deterministic analysis prepares evidence before AI reasoning","Preprocessing");
 const items=["Reversals","Action timing","Comparison pattern","Value ranking","Cross-signal checks"];
 items.forEach((v,i)=>{const x=64+i*230; shape(s,"roundRect",x,220,210,98,C.panel,"none","rounded-xl"); txt(s,v,x+16,240,178,34,16,C.ink,true,"center"); txt(s,["undo / redo","fast / deliberate","what was contrasted","where entities rank","which explanation survives"][i],x+16,278,178,22,12,C.faint,false,"center");});
 addArrow(s,246,365,760,C.blue);
 shape(s,"roundRect",275,420,730,122,C.pale,"none","rounded-xl"); txt(s,"STRUCTURED EVIDENCE PACKET",305,438,670,26,12,C.blue,true,"center"); txt(s,"The AI receives computed facts and relationships—not just a raw event stream.",305,475,670,42,22,C.ink,true,"center");
 footer(s,5); notes(s,"The application performs deterministic calculations first: reversals, timing, comparison behavior, and ranking against visible business values. The AI’s role is interpretation, not arithmetic or event reconstruction.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/CollaborationContextFormatter.cs"]);
}

// 6
{
 const s=p.slides.add(); header(s,6,"The agent evaluates competing explanations","Reasoning");
 card(s,64,214,350,330,"1  Generate hypotheses","Examples:\n• prefers graph view\n• expands for detail\n• focuses on highest-value options\n• is correcting an accidental action",C.blue);
 card(s,465,214,350,330,"2  Weigh evidence","Does the explanation fit the action sequence, timing, visible alternatives, current beliefs, and prior episodes?",C.cyan);
 card(s,866,214,350,330,"3  Update cautiously","Strengthen, weaken, revise, or leave the belief unchanged. Record what supports it and what would disprove it.",C.green);
 txt(s,"The output is a revisable explanation—not a declaration of user intent.",190,577,900,38,23,C.ink,true,"center");
 footer(s,6); notes(s,"This is the closest view of what the agent is ‘thinking.’ It compares competing explanations, weighs them against the current episode and memory, and produces a belief with explicit uncertainty and falsifiability.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/AgentCollaborationProfileUpdater.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/BeliefDocumentFormat.cs"]);
}

// 7
{
 const s=p.slides.add(); header(s,7,"Business values reveal what may be driving behavior","Data-aware learning");
 pill(s,"ILLUSTRATIVE",64,204,120,C.panel,C.faint);
 const rows=[{n:"Contract A",v:"$1.2M",rank:"#1",exp:"Expanded"},{n:"Contract B",v:"$740K",rank:"#2",exp:"Collapsed"},{n:"Contract C",v:"$310K",rank:"#3",exp:"Collapsed"}];
 txt(s,"VISIBLE OPTIONS",64,252,500,24,12,C.faint,true); txt(s,"VALUE",612,252,160,24,12,C.faint,true); txt(s,"RANK",806,252,120,24,12,C.faint,true); txt(s,"ACTION",990,252,180,24,12,C.faint,true);
 rows.forEach((r,i)=>{const y=285+i*76; shape(s,"roundRect",64,y,1120,58,i===0?C.pale:C.panel,"none","rounded-xl"); txt(s,r.n,84,y,430,58,18,C.ink,i===0); txt(s,r.v,612,y,160,58,18,C.ink,i===0); txt(s,r.rank,806,y,120,58,18,i===0?C.blue:C.muted,true); pill(s,r.exp,982,y+14,170,i===0?C.blue:C.white,i===0?C.white:C.faint);});
 txt(s,"Across episodes, the system can test whether ‘highest value’ predicts the user’s action better than position, label, or habit.",100,548,1080,62,21,C.ink,true,"center");
 footer(s,7); notes(s,"Use the highest-value-contract example. The system receives the values that were visible when the action occurred, calculates rank, and tests whether value is consistently predictive. The numbers shown here are illustrative, not production data.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderUI/src/collaboration/assembleSelectContractContext.ts","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/CollaborationContextFormatter.cs"]);
}

// 8
{
 const s=p.slides.add(); header(s,8,"Learning occurs through accumulation, contradiction, and revision","Belief lifecycle");
 const stages=[{t:"NOTICED",b:"A possible signal appears",c:C.faint},{t:"TENTATIVE",b:"Repeated, but alternatives remain",c:C.cyan},{t:"WORKING THEORY",b:"Predicts behavior across episodes",c:C.blue},{t:"SETTLED",b:"Strong and stable evidence",c:C.green}];
 stages.forEach((q,i)=>{const x=64+i*288; shape(s,"roundRect",x,245,250,170,C.panel,"none","rounded-xl"); shape(s,"rect",x,245,250,8,q.c,"none"); txt(s,q.t,x+20,276,210,24,12,q.c,true,"center"); txt(s,q.b,x+24,320,202,62,17,C.ink,true,"center"); if(i<3)addArrow(s,x+253,318,30,C.line);});
 shape(s,"roundRect",216,465,848,94,"#FFF4E5","none","rounded-xl"); txt(s,"CONTRADICTORY EVIDENCE",246,479,788,22,12,C.amber,true,"center"); txt(s,"can weaken, revise, split, or retire a belief at any stage",246,509,788,30,19,C.ink,true,"center");
 footer(s,8); notes(s,"Beliefs do not merely accumulate confidence. New episodes can contradict earlier conclusions. The agent can revise the wording, reduce conviction, distinguish contexts, or remove a belief when it no longer explains behavior.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilder.Data/Entities/Belief.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/BeliefDocumentFormat.cs"]);
}

// 9
{
 const s=p.slides.add(); header(s,9,"Short-term memory detects patterns; the profile preserves conclusions","Memory");
 shape(s,"roundRect",64,220,520,338,C.panel,"none","rounded-xl"); txt(s,"RECENT DECISION DIGESTS",94,244,460,28,13,C.faint,true); txt(s,"A compact sequence of recent episodes",94,288,460,34,22,C.ink,true); ["What happened","What data was visible","What evidence was derived","What the advisor returned"].forEach((v,i)=>{shape(s,"ellipse",96,352+i*42,18,18,C.cyan,"none"); txt(s,v,128,340+i*42,400,38,16,C.muted);});
 addArrow(s,603,374,70,C.blue);
 shape(s,"roundRect",696,220,520,338,C.pale,"none","rounded-xl"); txt(s,"DURABLE BELIEF PROFILE",726,244,460,28,13,C.blue,true); txt(s,"A structured, revisable user model",726,288,460,34,22,C.ink,true); ["Belief statement","Conviction and tenure","Supporting evidence","What would change the belief"].forEach((v,i)=>{shape(s,"ellipse",728,352+i*42,18,18,C.blue,"none"); txt(s,v,760,340+i*42,400,38,16,C.muted);});
 txt(s,"Recent detail supports learning; durable memory avoids replaying an unlimited history.",174,590,932,38,21,C.ink,true,"center");
 footer(s,9); notes(s,"The updater sees the current profile plus a bounded recent history. It does not resend every raw interaction forever. Recent digests expose patterns; the profile preserves the conclusions needed for future decisions.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilder.Data/Entities/TurnDigest.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilder.Data/Entities/BeliefDocument.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/AgentCollaborationProfileUpdater.cs"]);
}

// 10
{
 const s=p.slides.add(); header(s,10,"Learning and adaptation are separate responsibilities","Two-loop architecture");
 shape(s,"roundRect",64,220,530,320,C.panel,"none","rounded-xl"); pill(s,"LEARNING LOOP",92,246,138,C.white,C.blue); txt(s,"What is becoming true about this user?",92,296,450,58,24,C.ink,true); txt(s,"Episode + prior beliefs\n→ revise the durable profile\n→ preserve uncertainty and evidence",92,378,450,108,18,C.muted);
 shape(s,"roundRect",686,220,530,320,C.pale,"none","rounded-xl"); pill(s,"ADVISORY LOOP",714,246,140,C.blue,C.white); txt(s,"What should this application do now?",714,296,450,58,24,C.ink,true); txt(s,"Current context + current beliefs\n→ produce bounded recommendations\n→ pass through application policy",714,378,450,108,18,C.muted);
 addArrow(s,605,364,70,C.blue);
 txt(s,"The advisor can act on today’s profile while learning continues asynchronously for tomorrow’s decision.",126,580,1028,48,21,C.ink,true,"center");
 footer(s,10); notes(s,"The architecture deliberately separates updating the user model from advising the current UI. The advisor uses the most recently available profile; the profile update can continue independently so learning does not need to block every interaction.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/AgentCollaborationAdvisor.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/AgentCollaborationProfileUpdater.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/ObservationEndpoints.cs"]);
}

// 11
{
 const s=p.slides.add(); header(s,11,"Beliefs are translated into an application-owned contract","Controlled adaptation");
 step(s,64,240,250,1,"Belief","“Graph view is preferred for comparing contracts.”",C.cyan); addArrow(s,320,302,42,C.line);
 step(s,368,240,250,2,"Recommendation","Preferred layout: graph. Expand highest-value item.",C.blue); addArrow(s,624,302,42,C.line);
 step(s,672,240,250,3,"Policy","Check confidence, allowed actions, conflicts, and user control.",C.amber); addArrow(s,928,302,42,C.line);
 step(s,976,240,240,4,"View state","Apply a known, reversible application setting.",C.green);
 shape(s,"roundRect",188,472,904,88,C.panel,"none","rounded-xl"); txt(s,"The AI proposes intent through a bounded schema. The application remains responsible for execution.",226,490,828,50,22,C.ink,true,"center");
 footer(s,11); notes(s,"The AI never receives arbitrary control of the interface. It returns a recommendation in a schema the application owns. Policy decides whether the recommendation is allowed, and the application applies only known, reversible state changes.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilder.Data/Contracts/AdaptiveFrameworkContracts.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/AgentResultModels.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/AdaptationApprovalPolicy.cs"]);
}

// 12
{
 const s=p.slides.add(); header(s,12,"Uncertainty, user control, and failure are explicit states","Trust model");
 const data=[{t:"NO SUGGESTION",b:"Use the normal application default. Do not simulate an AI answer.",c:C.faint},{t:"LOW CONFIDENCE",b:"Observe more evidence or make only a subtle, reversible recommendation.",c:C.amber},{t:"CONTRADICTION",b:"Reassess the belief instead of forcing consistency.",c:C.red},{t:"MODEL FAILURE",b:"Preserve the existing profile and record diagnostics for investigation.",c:C.blue}];
 data.forEach((d,i)=>{const x=64+(i%2)*576,y=220+Math.floor(i/2)*175; card(s,x,y,544,142,d.t,d.b,d.c);});
 txt(s,"Default behavior is a valid outcome. Fabricated intelligence is not.",198,590,884,38,23,C.ink,true,"center");
 footer(s,12); notes(s,"This is a key design principle. If the advisor has no useful suggestion, the application keeps its ordinary state. There is no simulated AI fallback. Profile update failure preserves the existing profile, and diagnostic records make the failure visible.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/AgentCollaborationAdvisor.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/AgentCollaborationProfileUpdater.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/FileCollaborationAgentTranscriptLogger.cs"]);
}

// 13
{
 const s=p.slides.add(); header(s,13,"Every belief change and recommendation is reconstructable","Traceability");
 const stages=["Raw observations","Surface context","Derived evidence","Model request / response","Belief revision","Approved adaptation"];
 stages.forEach((v,i)=>{const x=64+(i%3)*384,y=220+Math.floor(i/3)*170; shape(s,"roundRect",x,y,344,126,i===4?C.pale:C.panel,"none","rounded-xl"); txt(s,String(i+1).padStart(2,"0"),x+18,y+16,36,24,12,i===4?C.blue:C.faint,true); txt(s,v,x+62,y+14,258,42,18,C.ink,true); txt(s,["what the user did","what the app knew","what was computed","what the agent considered","what changed and why","what the UI applied"][i],x+62,y+64,258,36,14,C.muted);});
 txt(s,"A correlated run record supports debugging, evaluation, and governance without guessing what happened.",136,577,1008,48,21,C.ink,true,"center");
 footer(s,13); notes(s,"For each run, the system can correlate the original observations, contextual snapshot, deterministic analysis, model interaction, profile outcome, advisor result, and applied decision. This is the foundation for evaluating behavior rather than relying on anecdotes.",["D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/AgentRunRecorder.cs","D:/Projects/AdaptiveWorkflow/src/AdaptiveTeamBuilderSvc/FileCollaborationAgentTranscriptLogger.cs"]);
}

// 14
{
 const s=p.slides.add(); header(s,14,"The POC establishes the architecture—and exposes the next evaluation questions","What comes next");
 const qs=["How quickly should a preference become actionable?","Which evidence should outweigh contradiction?","When should adaptation be silent, suggested, or automatic?","How well do beliefs transfer across surfaces and workflows?","What thresholds create trust for each class of change?","How do we measure usefulness without rewarding mere activity?"];
 qs.forEach((q,i)=>{const x=64+(i%2)*576,y=210+Math.floor(i/2)*116; shape(s,"roundRect",x,y,544,92,C.panel,"none","rounded-xl"); shape(s,"ellipse",x+18,y+25,42,42,i===5?C.blue:C.white,i===5?"none":C.line); txt(s,String(i+1),x+18,y+25,42,42,14,i===5?C.white:C.blue,true,"center"); txt(s,q,x+78,y+12,440,68,17,C.ink,true);});
 shape(s,"roundRect",180,586,920,52,C.pale,"none","rounded-xl"); txt(s,"The next phase is not more framework—it is measured learning quality, trust, and business value.",205,586,870,52,20,C.ink,true,"center");
 footer(s,14); notes(s,"Close by positioning the POC correctly: the core architecture exists. The important next work is evaluation—learning speed, precision, contradiction handling, cross-context transfer, user trust, and measurable value. The live demo is optional evidence, not the organizing narrative of this deck.");
}

await fs.mkdir(OUT,{recursive:true});
async function writeBlob(file,blob){ await fs.writeFile(file,new Uint8Array(await blob.arrayBuffer())); }
for (const [i,s] of p.slides.items.entries()) {
  const stem=`slide-${String(i+1).padStart(2,"0")}`;
  await writeBlob(path.join(OUT,`${stem}.png`),await p.export({slide:s,format:"png",scale:1}));
  const layout=await s.export({format:"layout"}); await fs.writeFile(path.join(OUT,`${stem}.layout.json`),await layout.text());
}
await writeBlob(path.join(OUT,"deck-montage.webp"),await p.export({format:"webp",montage:true,scale:1}));
const pptx=await PresentationFile.exportPptx(p); await pptx.save(FINAL);
console.log(`Wrote ${FINAL}`);
