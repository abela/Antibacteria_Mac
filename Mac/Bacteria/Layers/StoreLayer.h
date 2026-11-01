//
//  StoreLayer.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//
//
#import "GameMenuLayer.h"
#import "cocos2d.h"
#import <StoreKit/StoreKit.h>
//
@interface StoreLayer : GameMenuLayer <SKProductsRequestDelegate, SKPaymentTransactionObserver>
{
    CCLabelTTF *loadingLabel;
    CCSprite *loadingPricesSprite;
    NSMutableArray *buyButtons;
    NSArray *productIdsArray;
    CCLabelTTF *vitaminsCountTitle;
}
//
-(void) showMenu:(BOOL)showMenuFlag;
//
@end
