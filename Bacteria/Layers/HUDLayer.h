//
//  HUDLayer.h
//  GeorgianTale
//
//  Created by Giorgi Abelashvili on 1/15/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//
//
#import <Foundation/Foundation.h>
#import "cocos2d.h"
#import "MainGameLayer.h"
//
@interface HUDLayer : CCLayer {
	
	MainGameLayer *mainGameLayerDelegate;
    int keyCodeCounter;
}
//
@property (nonatomic, retain) MainGameLayer *mainGameLayerDelegate;
//
@end
