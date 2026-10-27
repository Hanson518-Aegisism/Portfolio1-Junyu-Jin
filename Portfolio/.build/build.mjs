import fs from 'node:fs/promises';
import {existsSync} from 'node:fs';
import path from 'node:path';
import {pathToFileURL} from 'node:url';
const runtime='C:/Users/Junyu Jin/.cache/codex-runtimes/codex-primary-runtime/dependencies';
process.env.RUNTIME_NODE_MODULES=runtime+'/node/node_modules';
process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES=process.env.RUNTIME_NODE_MODULES;
const {Presentation,PresentationFile}=await import(pathToFileURL(runtime+'/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs').href);
const root='D:/unityproject/Portfolio1 Junyu Jin';
const work=root+'/Portfolio';
const out=work+'/output';
const img=work+'/.build/images/';
const SKILL='C:/Users/Junyu Jin/.codex/plugins/cache/openai-primary-runtime/presentations/26.1025.11240/skills/presentations';
await fs.mkdir(out,{recursive:true});
const p=Presentation.create({slideSize:{width:1600,height:900}});
const C={bg:'#080D0F',white:'#ECEEE9',muted:'#A4AFAC',teal:'#83C5B5',red:'#A9433B',line:'#33413F'};
function text(s,t,x,y,w,h,size=26,color=C.white,bold=false,font='Microsoft YaHei'){
 const z=s.shapes.add({geometry:'textbox',name:t.slice(0,30),position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
 z.text=t;z.text.style={fontSize:size,typeface:font,color,bold,wrap:'square',autoFit:'none',insets:0,verticalAlignment:'top'};return z;
}
function rule(s,x,y,w,color=C.red){s.shapes.add({geometry:'rect',position:{left:x,top:y,width:w,height:2},fill:color,line:{fill:'none',width:0}});}
async function image(s,file,x,y,w,h,alt,crop){return s.images.add({blob:new Uint8Array(await fs.readFile(file)),contentType:'image/png',alt,position:{left:x,top:y,width:w,height:h},fit:'cover',...(crop?{crop}:{})});}
function base(n,eng,title){let s=p.slides.add();s.background.fill=C.bg;text(s,eng,64,44,1160,56,42,C.white,true,'Bahnschrift');text(s,title,64,107,1400,44,32,C.teal,true);rule(s,64,162,1472);text(s,'BURIED ECHOES   /   JUNYU JIN',64,863,1150,22,15,C.muted,false,'Bahnschrift');text(s,String(n).padStart(2,'0')+' / 06',1432,860,104,26,19,C.muted,false,'Bahnschrift');return s;}
function heading(s,k,title,x,y,w){text(s,k,x,y,w,25,17,C.red,true,'Bahnschrift');text(s,title,x,y+34,w,44,30,C.white,true);}
function notes(s,body){s.speakerNotes.text=body+'\nAll portfolio text and layout are editable. Reference images supplied by the user inform hierarchy and composition only. No reference artwork or watermarks are reproduced.';}

// 01. Project overview, large project image as the visual anchor.
{
const s=p.slides.add();s.background.fill=C.bg;
await image(s,img+'gallery.png',540,42,1018,802,'Unity capture of the timber-supported mine corridor');
text(s,'GAME DESIGN PORTFOLIO',64,68,440,32,22,C.teal,true,'Bahnschrift');rule(s,64,121,410);
text(s,'BURIED\nECHOES',64,184,480,196,83,C.white,true,'Bahnschrift');
text(s,'矿井逃生原型',68,410,440,58,39,C.teal,true);
text(s,'在狭窄、昏暗的矿井中，\n玩家通过观察环境与操作机关，\n判断继续前进的时机。',68,506,430,130,27);
text(s,'项目重点',68,683,430,30,20,C.muted,true);
text(s,'关卡布局 / 交互机制 / 氛围表达',68,726,435,70,23,C.white);
text(s,'Unity 2021.3   ·   URP   ·   第一人称原型',68,812,440,35,18,C.muted);
text(s,'JUNYU JIN',64,863,800,24,17,C.muted,false,'Bahnschrift');text(s,'01 / 06',1432,863,105,24,18,C.muted,false,'Bahnschrift');
notes(s,'Project working title: Buried Echoes, used in prior project context. Description is based on the current Unity Test scene and source scripts. Image: Portfolio/.build/images/gallery.png, temporary editor camera at the mine corridor, +1.35 EV capture exposure for readability. No original asset authorship is claimed.');
}
// 02. Intention and spatial composition.
{
const s=base(2,'DESIGN GOAL','封闭空间中的信息取舍');
await image(s,img+'mainhall.png',64,190,944,460,'Main hall scene showing low ceiling, warm lamps and receding fog');
text(s,'主厅实景：低矮顶面与局部照明压缩视野',64,664,944,31,21,C.muted);
await image(s,img+'mine.png',64,722,330,119,'Mine gallery view showing a post and a narrow opening');
text(s,'空间节奏',424,722,560,38,28,C.teal,true);text(s,'开敞区域提供观察距离，廊道与门洞重新收紧视线。木支架与岩壁建立可辨认的矿井结构。',424,768,565,76,23);
heading(s,'01  DESIGN INTENTION','让玩家先判断，再行动',1056,195,480);
text(s,'黑暗隐藏远处信息，灯具与结构轮廓保留近处参照。危险前的停顿成为探索节奏的一部分。',1056,281,470,120,26);
heading(s,'02  SPATIAL CONTRAST','可见区域与未知区域',1056,424,480);
text(s,'门洞、木柱与墙体遮挡下一段路径。玩家先看到局部空间，再通过移动获得完整信息。',1056,510,470,120,26);
heading(s,'03  PLAYER DECISION','环境成为判断依据',1056,653,480);
text(s,'玩家识别照明、机关与岔口位置，再决定交互、等待或绕行。',1056,739,470,95,26);
notes(s,'Source images: mainhall.png and mine.png, actual Unity Test scene captures. Design statements describe visual composition and intended player decisions, not playtest findings.');
}
// 03. Mechanics, image and explanatory text for each mechanic.
{
const s=base(3,'CORE MECHANICS','观察、操作与受限视野');
const cols=[64,565,1066];
await image(s,img+'ElectricTrap-(3).png',cols[0],190,470,270,'Electric trap area in the current Unity scene');
await image(s,img+'PipeTrap.png',cols[1],190,470,270,'Steam pipe assembly and three charge indicator lights');
await image(s,img+'entrance.png',cols[2],190,470,270,'Normal-vision scene used to establish why night vision provides useful information');
const blocks=[['01  ELECTRIC TRAP','开关改变危险状态','开关同步控制触发区与电力效果。玩家必须确认机关状态，再穿过危险区域。','设计作用：让交互结果可被观察'],['02  STEAM TRAP','蒸汽制造视线干扰','阀门启动后经历喷发、衰减与冷却。指示灯显示剩余次数，蒸汽遮挡视野并形成干扰。','设计作用：提供有限次数的环境操作'],['03  NIGHT VISION','短时获取额外信息','夜视系统包含佩戴过渡与有限电量。玩家选择何时开启，短时间内辨认黑暗中的环境和威胁。','配图：普通视野下的关卡环境']];
blocks.forEach((b,i)=>{heading(s,b[0],b[1],cols[i],490,470);text(s,b[2],cols[i],582,470,150,25);text(s,b[3],cols[i],744,470,62,20,C.teal);});
notes(s,'Mechanic sources: Assets/Scripts/KillTrapController.cs and TrapSwitch.cs; SteamTrapController.cs, SteamVisionObscurer.cs, SteamValveInteract.cs; NightVision/NightVisionController.cs and NightVisionMark.cs. Steam obstructs vision; no AI stun, damage, or enemy blind effect is claimed. Night vision image is explicitly normal vision rather than fabricated gameplay. Default timings were deliberately omitted because scene overrides may differ.');
}
// 04. Existing character asset and the implemented behaviour.
{
const s=base(4,'ENEMY BEHAVIOR','Tomb Soul：追击与丢失目标后的搜索');
const idle=existsSync(img+'character-Idle.png')?img+'character-Idle.png':root+'/Assets/Tomb-Soul/TombSoul_Idle.png';
const crawl=existsSync(img+'character-Crawl.png')?img+'character-Crawl.png':root+'/Assets/Tomb-Soul/TombSoul_Preview.png';
await image(s,idle,550,190,430,595,'Existing Tomb Soul asset in idle pose',idle.includes('.build')?undefined:{left:.28,right:.28,top:.06,bottom:.06});
await image(s,crawl,1025,190,510,310,'Existing Tomb Soul asset in crawl pose',crawl.includes('.build')?undefined:{left:.26,right:.26,top:.34,bottom:.33});
heading(s,'01  IDLE','等待目标进入视野',64,197,440);text(s,'没有有效目标时保持待机，播放静止动作，为玩家保留观察空隙。',64,283,430,110,25);
heading(s,'02  CHASE','看见玩家后追击',64,423,440);text(s,'距离与视线检测共同决定追击。导航代理控制位移，移动速度驱动爬行动画播放。',64,509,430,122,25);
heading(s,'03  INVESTIGATE','前往最后可见位置',64,654,440);text(s,'视线中断后搜索最后位置。搜索超时后回到待机，形成可读的行为变化。',64,740,430,100,25);
text(s,'动作与反馈',1025,536,510,42,32,C.teal,true);text(s,'Idle 对应静止，Crawl 对应移动。\n追击可配置一次性的 3D 尖叫声，\n接近玩家后触发现有死亡逻辑。',1025,593,510,125,25);
rule(s,1025,752,510,C.line);text(s,'资产说明：使用现成 Tomb Soul 角色与动画，\n本页重点为行为逻辑及项目接入。',1025,777,510,64,20,C.muted);
notes(s,'Existing asset source: Assets/Tomb-Soul. Original model and Idle/Crawl animations are third-party project resources; no original character modelling is claimed. Behaviour verified against Assets/Scripts/EnemyChasePlayer.cs and Assets/Tomb-Soul/README_使用说明.md. No attack or death animations are claimed.');
}
// 05. Original concept map is preserved and explicitly identified as a concept.
{
const s=base(5,'LEVEL BREAKDOWN','以主厅为核心的环路探索方案');
const mapImage=await image(s,root+'/blackwell-mine-optimized-level-map.png',64,188,1050,614,'Existing Blackwell Mine level concept map showing the office, main hall, tunnel, machine room, ring and exit');mapImage.fit='contain';
heading(s,'01  CENTRAL HUB','主厅承担空间参照',1160,196,375);text(s,'主厅连接不同功能区，让玩家通过重复经过的位置建立方向感。',1160,280,370,104,24);
heading(s,'02  OPTIONAL ROUTE','环路提供绕行选择',1160,414,375);text(s,'矿区侧环与侧向连接构成备选路线。遇到压力时，玩家可以重新判断路径。',1160,498,370,130,24);
heading(s,'03  ESCAPE DIRECTION','出口形成前进目标',1160,663,375);text(s,'通风井出口为探索提供方向，机关节点改变局部通路状态。',1160,747,370,89,24);
text(s,'既有关卡概念图：展示动线方案，不代表当前场景的实测平面或已完成内容。',76,817,1024,26,17,C.muted);
notes(s,'Source: blackwell-mine-optimized-level-map.png, an existing project concept image. Its embedded labels, date and speed values belong to that image. Those values are not presented as verified current game settings. Concept flow and current Unity scene captures are intentionally distinguished.');
}
// 06. Required time comparison uses the same camera and source sequence deliberately.
{
const s=base(6,'ATMOSPHERE & VISUAL GUIDANCE','在黑暗中保留方向，在烟雾中建立深度');
await image(s,root+'/Library/Atmosphere-Smoke-A.png',64,191,718,430,'Same gameplay view at smoke time zero seconds');
await image(s,root+'/Library/Atmosphere-Smoke-B.png',818,191,718,430,'Same gameplay view at smoke time eight seconds');
text(s,'00s   固定视角',64,634,718,28,20,C.teal,true);text(s,'08s   烟团位置与轮廓变化',818,634,718,28,20,C.teal,true);
text(s,'局部灯光保留空间参照',64,695,718,42,31,C.white,true);text(s,'墙灯照亮木梁与门洞边缘，近处结构保持可辨认。明暗间隔帮助视线沿通道向远处移动。',64,754,718,88,25);
text(s,'有空隙的烟团强化距离',818,695,718,42,31,C.white,true);text(s,'三维密度变化让烟雾缓慢漂移，形成疏密交替。浓度、烟团大小和速度可独立调节。',818,754,718,88,25);
notes(s,'Source: Library/Atmosphere-Smoke-A.png and Atmosphere-Smoke-B.png, verified same-camera renders at 0 and 8 seconds from the actual project raymarched fog shader. This intentional repeated-view comparison demonstrates temporal variation. Sources: SceneFogController.cs and VolumetricFog.shader. No frame-rate or performance claim is made.');
}

for(let i=0;i<p.slides.items.length;i++){
 const s=p.slides.items[i];
 await fs.writeFile(out+`/page-${String(i+1).padStart(2,'0')}.png`,new Uint8Array(await (await p.export({slide:s,format:'png',scale:1})).arrayBuffer()));
 console.log('Rendered page',i+1);
}
await fs.writeFile(work+'/.build/deck.proto.json',JSON.stringify(p.toProto()));
const candidate=work+'/.build/candidate.pptx';await(await PresentationFile.exportPptx(p)).save(candidate);
const {finalizePresentation}=await import(pathToFileURL(SKILL+'/container_tools/artifact_tool_utils.mjs').href);
const result=await finalizePresentation({workspaceDir:work,candidatePath:candidate,finalPath:out+'/Buried-Echoes-Portfolio-16x9.pptx',pythonExecutable:runtime+'/python/python.exe',integrityValidatorPath:SKILL+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:SKILL+'/container_tools/inspect_presentation_layout_geometry.py',explicitTotalSlideCount:6,layoutArgs:['--expected-slide-size-emu','15240000,8572500','--validate-heading-fit'],requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],fontPolicy:{basis:'design',families:['Microsoft YaHei','Bahnschrift']},verifyArtifactToolImport:true,receiptPath:work+'/.build/validation.json'});
console.log(JSON.stringify(result));
try {await fs.writeFile(out+'/Buried-Echoes-Portfolio-16x9.pdf',new Uint8Array(await(await p.export({format:'pdf'})).arrayBuffer()));console.log('PDF exported');}catch(e){console.log('PDF unavailable:',e.message);}
