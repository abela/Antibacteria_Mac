//
//  Camera.h
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/14/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "cocos2d.h"

@class MainGameLayer;
@interface Camera : NSObject
{
    MainGameLayer *gameLayer;
    CCFollow *folow;
    CGPoint cameraPosition;
    CGPoint lastPos;
    CGPoint xFrozenFlags;
    CGPoint yFrozenFlags;
    CGPoint frozenLastPoints;
}
//
@property (nonatomic,assign) CGPoint cameraPosition;
//
-(id) initWithGameLayer:(MainGameLayer*)g;
-(void) updateCameraWithCharacter:(CCSprite*)character;
-(void) restart;
//
@end
