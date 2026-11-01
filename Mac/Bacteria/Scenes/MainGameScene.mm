//
//  MainGameScene.mm
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import "MainGameScene.h"
#import "HUDLayer.h"
#import "MainGameLayer.h"
#import "LevelOverLayer.h"
#import "Figure.h"

@implementation MainGameScene

- (id) init
{
	if(self == [super init])
	{
		//initialize layers
		[self initLayers];
		
		return self;
	}
	return nil;
}
//
- (void) initLayers
{
	//initialize main game layer 
	mainLayer = [MainGameLayer node];
	//[mainLayer removeAllChildrenWithCleanup:YES];
	mainLayer.parent = nil;
	[self addChild:mainLayer z:zMainGameSceneLayer tag:zMainGameSceneLayer];
	//
	//initilize level over layer
	levelOverLayer = [LevelOverLayer node];
	//[levelOverLayer removeAllChildrenWithCleanup:YES];
	levelOverLayer.parent = nil;
	[self addChild:levelOverLayer z:zLevelOverLayer tag:zLevelOverLayer];
	//
	//initialize main game over layer
	hudLayer = [HUDLayer node];
	//uiLayer.parent = nil;
	//mainLayer.delegate = hudLayer;
	hudLayer.mainGameLayerDelegate = mainLayer;
	mainLayer.levelOverLayerDelegate = levelOverLayer;
	levelOverLayer.mainGameLayerdelegate = mainLayer;
	[self addChild:hudLayer z:zUserInterfaceLayer tag:zUserInterfaceLayer];
    //
}
//
-(void) dealloc
{
    [super dealloc];
    [mainLayer removeFromParentAndCleanup:YES];
    [levelOverLayer removeFromParentAndCleanup:YES];
    mainLayer = nil;
    levelOverLayer = nil;
    hudLayer = nil;
}
@end
