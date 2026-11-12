import bpy, math, os
from mathutils import Vector

# Cute, original chibi fantasy swordsman. All dimensions are in Blender units.
OUT = os.environ.get("OUTPUT_DIR", "build")
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.materials, bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.actions):
    pass

def mat(name, color, metallic=0.0, roughness=0.48):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get("Principled BSDF"); p.inputs["Base Color"].default_value=(*color,1)
    p.inputs["Metallic"].default_value=metallic; p.inputs["Roughness"].default_value=roughness
    return m
skin=mat("Warm peach skin",(0.98,.66,.48)); skin2=mat("Ear blush",(1,.40,.39))
hair=mat("Chestnut hair",(.20,.075,.035)); hairlight=mat("Hair highlights",(.38,.15,.065))
blue=mat("Adventurer blue tunic",(.10,.31,.68)); blue2=mat("Tunic trim",(.35,.68,1))
gold=mat("Warm gold",(.95,.62,.12),.55,.28); leather=mat("Boot leather",(.22,.085,.035))
leather2=mat("Leather straps",(.42,.20,.08)); white=mat("Eye whites",(1,.96,.84))
iris=mat("Deep sapphire eyes",(.015,.10,.30),.12,.2); pupil=mat("Pupils",(.008,.012,.025),.05,.12)
metal=mat("Sword steel",(.63,.78,.90),.82,.22); dark=mat("Sword grip",(.12,.045,.025))
red=mat("Scarf red",(.75,.055,.08)); green=mat("Tiny gem",(.04,.75,.38),.35,.2)

# Armature: stylized proportions, simple deform rig.
arm_data=bpy.data.armatures.new("Chibi Adventurer Rig")
rig=bpy.data.objects.new("Chibi Adventurer | Rig",arm_data); bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig; rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
bones={
 "root":((0,0,0),(0,0,.72),None),
 "pelvis":((0,0,.72),(0,0,1.18),"root"),
 "spine":((0,0,1.18),(0,0,1.95),"pelvis"),
 "head":((0,0,1.95),(0,0,2.92),"spine"),
 "upper_arm.L":((-.40,0,1.78),(-.72,0,1.42),"spine"),
 "forearm.L":((-.72,0,1.42),(-.78,-.01,1.12),"upper_arm.L"),
 "upper_arm.R":((.40,0,1.78),(.72,0,1.42),"spine"),
 "forearm.R":((.72,0,1.42),(.78,-.01,1.12),"upper_arm.R"),
 "thigh.L":((-.20,0,.90),(-.23,0,.53),"pelvis"),
 "shin.L":((-.23,0,.53),(-.24,-.01,.13),"thigh.L"),
 "thigh.R":((.20,0,.90),(.23,0,.53),"pelvis"),
 "shin.R":((.23,0,.53),(.24,-.01,.13),"thigh.R"),
}
for name,(h,t,parent) in bones.items():
 b=arm_data.edit_bones.new(name); b.head=h; b.tail=t
 if parent: b.parent=arm_data.edit_bones[parent]
bpy.ops.object.mode_set(mode="OBJECT"); rig.select_set(False)

def bind(obj, bone):
    vg=obj.vertex_groups.new(name=bone); vg.add(list(range(len(obj.data.vertices))),1.0,"REPLACE")
    mod=obj.modifiers.new("Character deformation","ARMATURE"); mod.object=rig
    obj.parent=rig
    return obj

def finish(obj,name,material,bone):
    obj.name=name
    if material: obj.data.materials.append(material)
    for p in obj.data.polygons: p.use_smooth=True
    return bind(obj,bone)

def uv(name,loc,scale,material,bone="spine",segments=24,rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    o=bpy.context.object; o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,material,bone)

def cube(name,loc,scale,material,bone="spine",bevel=.08):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.dimensions=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new("Soft rounded edges","BEVEL"); mod.width=bevel; mod.segments=3
        o.modifiers.new("Weighted corner normals","WEIGHTED_NORMAL")
    return finish(o,name,material,bone)

def rod(name,a,b,radius,material,bone,vertices=16):
    a,b=Vector(a),Vector(b); d=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=d.length, location=(a+b)/2)
    o=bpy.context.object; o.rotation_mode="QUATERNION"; o.rotation_quaternion=d.to_track_quat("Z","Y")
    return finish(o,name,material,bone)

# Boots and legs
for side,x,bone1,bone2 in [("L",-.23,"thigh.L","shin.L"),("R",.23,"thigh.R","shin.R")]:
    uv("Rounded boot "+side,(x,-.015,.20),(.22,.31,.22),leather,bone2)
    cube("Boot cuff "+side,(x,-.005,.39),(.32,.34,.18),gold,bone2,.07)
    uv("Shorts leg "+side,(x,0,.77),(.22,.22,.30),blue,bone1)
# torso, tunic, belt, collar
uv("Tunic body",(0,0,1.52),(.48,.32,.53),blue,"spine")
uv("Tunic hem",(0,0,1.13),(.49,.33,.12),blue2,"pelvis")
cube("Gold belt",(0,-.005,1.22),(.83,.12,.12),gold,"pelvis",.045)
uv("Belt buckle",(0,-.13,1.22),(.12,.045,.12),gold,"pelvis")
uv("Chest scarf",(0,-.28,1.83),(.28,.075,.16),red,"spine")
# arms and gloves
for side,x,upper,fore in [("L",-.58,"upper_arm.L","forearm.L"),("R",.58,"upper_arm.R","forearm.R")]:
    uv("Shoulder puff "+side,(x,0,1.75),(.23,.24,.24),blue2,"spine")
    rod("Sleeve "+side,(x,0,1.72),(x*1.20,0,1.43),.16,blue,upper)
    rod("Forearm "+side,(x*1.20,0,1.43),(x*1.30,-.01,1.17),.115,skin,fore)
    uv("Detailed palm "+side,(x*1.30,-.055,1.12),(.145,.125,.135),skin,fore)
# head, ears, face
uv("Large cute head",(0,-.015,2.43),(.64,.53,.65),skin,"head",32,24)
for side,x in [("L",-.62),("R",.62)]:
    uv("Ear "+side,(x,-.005,2.43),(.15,.13,.21),skin,"head")
    uv("Ear inner "+side,(x,-.115,2.43),(.075,.035,.12),skin2,"head")
# hair cap and locks
uv("Hair cap",(0,.015,2.83),(.67,.55,.40),hair,"head",32,20)
for i,(x,z,s) in enumerate([(-.43,2.72,.25),(-.20,2.70,.24),(0,2.68,.25),(.22,2.70,.24),(.44,2.73,.22)]):
    uv("Front fringe %02d"%i,(x,-.43,z),(s,.16,.30),hair if i%2 else hairlight,"head")
uv("Back hair",(0,.34,2.49),(.57,.28,.53),hair,"head")
# eyes oriented toward -Y, with eye whites, iris and highlights
for side,x in [("L",-.25),("R",.25)]:
    uv("Eye white "+side,(x,-.492,2.48),(.155,.055,.19),white,"head")
    uv("Blue iris "+side,(x,-.542,2.47),(.105,.035,.135),iris,"head")
    uv("Pupil "+side,(x,-.570,2.47),(.060,.022,.095),pupil,"head")
    uv("Eye sparkle "+side,(x-.027,-.592,2.53),(.032,.014,.04),white,"head",16,12)
    uv("Cheek blush "+side,(x*1.75,-.488,2.27),(.09,.018,.045),skin2,"head",16,12)
# tiny nose and smile curve
uv("Tiny nose",(0,-.54,2.34),(.045,.035,.035),skin2,"head",16,12)
curve=bpy.data.curves.new("Friendly smile","CURVE"); curve.dimensions="3D"; curve.bevel_depth=.012; curve.bevel_resolution=3
sp=curve.splines.new("BEZIER"); sp.bezier_points.add(2)
for p,co in zip(sp.bezier_points,[(-.09,-.535,2.25),(0,-.552,2.22),(.09,-.535,2.25)]):
    p.co=co; p.handle_left_type="AUTO"; p.handle_right_type="AUTO"
sm=bpy.data.objects.new("Friendly smile",curve); bpy.context.collection.objects.link(sm); curve.materials.append(dark)
bpy.ops.object.select_all(action="DESELECT"); sm.select_set(True); bpy.context.view_layer.objects.active=sm
bpy.ops.object.convert(target="MESH"); sm=bpy.context.object; bind(sm,"head")
# Facial expression: sculpted, clearly readable eyebrows with tapered-looking ends.
for side,x in [("L",-.25),("R",.25)]:
    brow=uv("Expressive eyebrow "+side,(x,-.535,2.685),(.155,.035,.045),hairlight,"head",20,12)
    brow.rotation_euler[1]=math.radians(8 if side=="L" else -8)
    # Subtle lower eyelid gives the large eyes a more finished, animated look.
    lid=uv("Lower eyelid rim "+side,(x,-.535,2.335),(.145,.025,.025),skin2,"head",20,12)

# Individual toes emerging from the front of the boots; each has a small nail plate.
for side,x,bone in [("L",-.23,"shin.L"),("R",.23,"shin.R")]:
    for i,offset in enumerate([-.105,-.035,.035,.105]):
        tx=x+offset
        length=.078 if i in (0,3) else .088
        uv("Toe %s %d"%(side,i+1),(tx,-.265,.105),(length,.105,.060),skin,bone,20,12)
        nail=uv("Toenail %s %d"%(side,i+1),(tx,-.354,.119),(length*.48,.014,.024),white,bone,16,10)
        nail.rotation_euler[1]=math.radians((i-1.5)*3)
    # Leather instep straps frame the exposed toes instead of hiding them.
    rod("Sandal strap "+side,(x-.17,-.22,.205),(x+.17,-.22,.205),.035,leather2,bone)

# Detailed fingers and nails. The right hand is deliberately wrapped around the
# vertical sword grip; the four fingers cross its front and the thumb closes over it.
# Right palm is positioned around the handle at x=.80, z=1.10.
for i,z in enumerate([1.045,1.085,1.125,1.165]):
    finger=uv("Sword hand curled finger %d"%(i+1),(.785,-.158,z),(.105,.060,.024),skin,"forearm.R",20,12)
    nail=uv("Sword hand fingernail %d"%(i+1),(.795,-.211,z+.002),(.034,.012,.014),white,"forearm.R",16,10)
    nail.rotation_euler[1]=math.radians(-8)
# Thumb crosses diagonally over the upper fingers and visibly clamps the hilt.
thumb=uv("Sword hand opposing thumb",(.865,-.165,1.185),(.095,.060,.035),skin,"forearm.R",20,12)
thumb.rotation_euler[1]=math.radians(-32)
uv("Sword thumb nail",(.885,-.218,1.192),(.032,.012,.015),white,"forearm.R",16,10)
# Small knuckle pads create a readable closed fist silhouette.
for i,z in enumerate([1.045,1.085,1.125,1.165]):
    uv("Sword grip knuckle %d"%(i+1),(.705,-.145,z),(.027,.045,.025),skin,"forearm.R",16,10)

# Left hand: relaxed separated fingers with visible nail plates.
for i,z in enumerate([1.045,1.085,1.125,1.165]):
    x=.0 # overwritten below for left-hand placement
    fx=-.754-(i-1.5)*.025
    finger=uv("Left hand finger %d"%(i+1),(fx,-.155,z),(.030,.060,.070),skin,"forearm.L",18,12)
    nail=uv("Left hand fingernail %d"%(i+1),(fx,-.211,z-.005),(.018,.012,.027),white,"forearm.L",16,10)
uv("Left hand thumb",(-.855,-.15,1.185),(.045,.06,.085),skin,"forearm.L",18,12)
uv("Left thumb nail",(-.884,-.205,1.19),(.018,.012,.03),white,"forearm.L",16,10)

# headband and center gem
uv("Headband",(0,-.05,2.91),(.62,.51,.105),gold,"head")
uv("Headband jewel",(0,-.55,2.91),(.10,.045,.10),green,"head")
# shoulder armor
for side,x in [("L",-.48),("R",.48)]:
    uv("Gold shoulder pauldron "+side,(x,-.025,1.86),(.23,.25,.17),gold,"spine")
# short sword in right hand (blade points up)
rod("Sword grip",(.78,-.04,1.10),(.80,-.04,.82),.045,dark,"forearm.R")
rod("Sword guard",(.66,-.045,1.09),(.94,-.045,1.09),.045,gold,"forearm.R")
rod("Sword blade",(.80,-.045,1.12),(.80,-.045,1.80),.075,metal,"forearm.R",4)
uv("Sword tip",(.80,-.045,1.82),(.075,.075,.10),metal,"forearm.R",12,8)
# face the front (-Y) with a subtle three-quarter turn
rig.rotation_euler[2]=math.radians(-8)
# animations: gentle idle breathing via spine/head and alternating walk legs/arms
bpy.context.view_layer.objects.active=rig
rig.animation_data_create()
def key_pose(frame, pose):
    bpy.context.scene.frame_set(frame)
    for bn,rot in pose.items():
        pb=rig.pose.bones.get(bn)
        if pb:
            pb.rotation_mode="XYZ"; pb.rotation_euler=rot
            pb.keyframe_insert(data_path="rotation_euler",frame=frame,group=bn)
idle={
 1:{"spine":(0,0,0),"head":(0,0,0),"upper_arm.L":(0,0,-.04),"upper_arm.R":(0,0,.04)},
 10:{"spine":(.025,0,0),"head":(-.02,0,0),"upper_arm.L":(0,0,-.04),"upper_arm.R":(0,0,.04)},
 20:{"spine":(0,0,0),"head":(0,0,0),"upper_arm.L":(0,0,-.04),"upper_arm.R":(0,0,.04)},
}
for f,p in idle.items(): key_pose(f,p)
if rig.animation_data and rig.animation_data.action:
    rig.animation_data.action.name="Idle | Gentle breathing"
    for fc in rig.animation_data.action.fcurves:
        for kp in fc.keyframe_points: kp.interpolation="BEZIER"
idle_action=rig.animation_data.action

# Separate gameplay clips. Each clip is keyed on the same compact humanoid rig.
# FBX export below bakes all actions so Unity can select Idle, Walk and Attack.
def make_action(name, frames):
    action=bpy.data.actions.new(name)
    rig.animation_data.action=action
    for frame, pose in frames:
        key_pose(frame, pose)
    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation="BEZIER"
    return action

walk=make_action("Walk | Loop",[
 (1, {"thigh.L":(0.42,0,0),"thigh.R":(-0.42,0,0),
      "shin.L":(-0.18,0,0),"shin.R":(0.18,0,0),
      "upper_arm.L":(-0.20,0,-0.05),"upper_arm.R":(0.20,0,0.05),
      "spine":(0.025,0,0)}),
 (7, {"thigh.L":(0,0,0),"thigh.R":(0,0,0),
      "shin.L":(0.08,0,0),"shin.R":(0.08,0,0),
      "upper_arm.L":(0,0,-0.05),"upper_arm.R":(0,0,0.05),
      "spine":(-0.015,0,0)}),
 (13, {"thigh.L":(-0.42,0,0),"thigh.R":(0.42,0,0),
       "shin.L":(0.18,0,0),"shin.R":(-0.18,0,0),
       "upper_arm.L":(0.20,0,-0.05),"upper_arm.R":(-0.20,0,0.05),
       "spine":(0.025,0,0)}),
 (19, {"thigh.L":(0,0,0),"thigh.R":(0,0,0),
       "shin.L":(0.08,0,0),"shin.R":(0.08,0,0),
       "upper_arm.L":(0,0,-0.05),"upper_arm.R":(0,0,0.05),
       "spine":(-0.015,0,0)}),
 (25, {"thigh.L":(0.42,0,0),"thigh.R":(-0.42,0,0),
       "shin.L":(-0.18,0,0),"shin.R":(0.18,0,0),
       "upper_arm.L":(-0.20,0,-0.05),"upper_arm.R":(0.20,0,0.05),
       "spine":(0.025,0,0)}),
])
attack=make_action("Attack | Sword Slash",[
 (1, {"spine":(0,0,0),"upper_arm.R":(0.05,0,-0.18),
      "forearm.R":(0,0,0),"head":(0,0,0)}),
 (5, {"spine":(-0.12,0,-0.08),"upper_arm.R":(-0.25,0,-0.35),
      "forearm.R":(-0.10,0,0.10),"head":(0.08,0,0)}),
 (9, {"spine":(0.10,0,0.10),"upper_arm.R":(0.45,0,-0.55),
      "forearm.R":(0.25,0,0.25),"head":(-0.05,0,0)}),
 (14, {"spine":(0,0,0),"upper_arm.R":(0.05,0,-0.18),
       "forearm.R":(0,0,0),"head":(0,0,0)}),
])
# Return the saved Blender scene to the neutral idle pose before preview/export.
rig.animation_data.action=idle_action
# Save native blend with camera/light/stage for easy preview
bpy.ops.object.select_all(action="DESELECT")
rig.select_set(True); bpy.context.view_layer.objects.active=rig
bpy.ops.object.camera_add(location=(4.4,-8.5,3.5))
cam=bpy.context.object; cam.name="Preview Camera"
direction=Vector((0,0,1.55))-cam.location; cam.rotation_euler=direction.to_track_quat("-Z","Y").to_euler()
cam.data.lens=55; bpy.context.scene.camera=cam
bpy.ops.object.light_add(type="AREA", location=(1,-4,6)); key=bpy.context.object; key.name="Softbox Key"; key.data.energy=520; key.data.shape="DISK"; key.data.size=5
key.rotation_euler=(math.radians(25),0,math.radians(-12))
bpy.ops.object.light_add(type="AREA", location=(-4,-1,3)); fill=bpy.context.object; fill.name="Softbox Fill"; fill.data.energy=260; fill.data.size=4
fill.rotation_euler=(math.radians(35),0,math.radians(-35))
scene=bpy.context.scene
scene.render.engine="BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys() else "BLENDER_EEVEE"
scene.render.resolution_x=900; scene.render.resolution_y=900; scene.render.resolution_percentage=100
scene.render.image_settings.file_format="PNG"; scene.render.film_transparent=False
scene.world.color=(.055,.065,.09)
scene.frame_start=1; scene.frame_end=20; scene.render.fps=24
scene.view_settings.view_transform="Standard"
# Select character meshes and rig for export; exclude camera and lights.
bpy.ops.object.select_all(action="DESELECT")
for o in bpy.context.scene.objects:
    if o==rig or o.parent==rig or o.type=="MESH" and o.find_armature()==rig or o.type=="CURVE" and o.parent==rig:
        o.select_set(True)
rig.select_set(True); bpy.context.view_layer.objects.active=rig
blend=os.path.join(OUT,"Cute_Ragnarok_Inspired_Chibi_Swordsman.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend)
# FBX copy for Unity; keep armature + weighted character meshes.
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,"Cute_Ragnarok_Inspired_Chibi_Swordsman.fbx"),
    use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
    object_types={"ARMATURE","MESH"}, add_leaf_bones=False, bake_anim=True,
    bake_anim_use_all_bones=True, bake_anim_force_startend_keying=True)
print("Created:",blend)
