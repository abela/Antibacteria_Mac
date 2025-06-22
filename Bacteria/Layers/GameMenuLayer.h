//
//  GameLayer.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "CCLayer.h"
#import "CCScene.h"
#import "CCMenu.h"

@interface GameMenuLayer : CCLayer
{
    CCMenu *menu;
}
+(CCScene*)scene;
-(void) createLayerMenu;
@end
