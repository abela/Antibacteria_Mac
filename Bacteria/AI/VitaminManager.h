//
//  VitaminManager.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 6/20/13.
//
//

#import <Foundation/Foundation.h>
#import <vector>

using namespace std;
@class Vitamin;
@interface VitaminManager : NSObject
{
    int vitaminsCount;
    vector <Vitamin*> vitamins;
}
//
@property (readonly) int vitaminsCount;
//
-(CGPoint) getValidBonusSpawnPointAtPoint:(CGPoint)point;
-(void) createVitaminAtPosition:(CGPoint)position withScore:(int)score;
+(VitaminManager*) sharedVitaminManager;
-(void) releaseVitaminManager;
-(void) update;
@end
