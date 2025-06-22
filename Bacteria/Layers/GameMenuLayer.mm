//
//  GameLayer.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "GameMenuLayer.h"
#import "CCTextureCache.h"

@implementation GameMenuLayer
+(CCScene*)scene
{
    return nil;
}
//
-(void) createLayerMenu
{
    
}
//
-(void) onExit
{
    [self removeChild:menu cleanup:YES];
    [[CCTextureCache sharedTextureCache] removeUnusedTextures];
}
@end
