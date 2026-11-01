//
//  InAppManager.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/30/13.
//
//

#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>
#include <vector>
using namespace std;
//
@interface StoreProductData : NSObject
{
    NSString *productId;
    NSString *productDescription;
    NSString *price;
}
//
@property (nonatomic,retain) NSString *productId;
@property (nonatomic,retain) NSString *productDescription;
@property (nonatomic,retain) NSString *price;
@end
//
@class StoreLayer;
@interface InAppManager : NSObject <SKProductsRequestDelegate,
                                    SKPaymentTransactionObserver>
{
    vector<StoreProductData*> downloadedProducts;
    StoreLayer *storeLayer;
}
@property (nonatomic,retain) StoreLayer *storeLayer;
@property (nonatomic,readonly) vector<StoreProductData*> downloadedProducts;
+(InAppManager*)sharedInAppManager;
-(void) startGettingProducts;
-(void) buyProductWithIdentifier:(NSString*)identifier;
-(void) buyProductWithId:(NSString*)identifier;
@end
//
