//
//  MainGameScene.h
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "cocos2d.h"
#import "Scene.h"

@class MainGameLayer;
@class MainGameOverLayer;
@class LevelOverLayer;
@class HUDLayer;
@interface MainGameScene : Scene {

	MainGameLayer *mainLayer;
	MainGameOverLayer *overLayer;
	LevelOverLayer *levelOverLayer;
	HUDLayer *hudLayer;
}


@end
